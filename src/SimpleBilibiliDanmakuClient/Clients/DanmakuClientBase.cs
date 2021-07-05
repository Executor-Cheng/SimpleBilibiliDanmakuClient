using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleBilibiliDanmakuClient.Models;
#if NETSTANDARD2_0
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Buffers.Binary;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
#endif

namespace SimpleBilibiliDanmakuClient.Clients
{
    public abstract partial class DanmakuClientBase : IDanmakuClient
    {
        protected abstract byte Version { get; }
#if !NETSTANDARD2_0
        protected static readonly Memory<byte> heartBeatPacket = new byte[16] { 0, 0, 0, 16, 0, 16, 0, 2, 0, 0, 0, 2, 0, 0, 0, 1 };
#else
        protected static readonly byte[] heartBeatPacket = new byte[16] { 0, 0, 0, 16, 0, 16, 0, 2, 0, 0, 0, 2, 0, 0, 0, 1 };
#endif
        protected static byte[] CreatePayload(int action)
        {
            byte[] buffer = new byte[16];
            ref BilibiliDanmakuProtocol protocol = ref Interpret(buffer);
            protocol.PacketLength = buffer.Length;
            protocol.Action = action;
            protocol.Magic = 16;
            protocol.Parameter = 1;
            protocol.Version = 2;
            protocol.ChangeEndian();
            return buffer;
        }

        protected static byte[] CreatePayload(int action, string body)
        {
            byte[] buffer = new byte[16 + Encoding.UTF8.GetByteCount(body)];
#if !NETSTANDARD2_0
            Span<byte> span = buffer;
            ref BilibiliDanmakuProtocol protocol = ref Unsafe.As<byte, BilibiliDanmakuProtocol>(ref MemoryMarshal.GetReference(span));
#else
            ref BilibiliDanmakuProtocol protocol = ref Interpret(buffer);
#endif
            protocol.PacketLength = buffer.Length;
            protocol.Action = action;
            protocol.Magic = 16;
            protocol.Parameter = 1;
            protocol.Version = 2;
            protocol.ChangeEndian();
#if !NETSTANDARD2_0
            Encoding.UTF8.GetBytes(body, span[16..]);
#else
            Encoding.UTF8.GetBytes(body, 0, body.Length, buffer, 16);
#endif
            return buffer;
        }

        protected static byte[] CreatePayload(int action, byte[] body)
        {
            byte[] buffer = new byte[16 + body.Length];
#if !NETSTANDARD2_0
            Span<byte> span = buffer;
            ref BilibiliDanmakuProtocol protocol = ref Unsafe.As<byte, BilibiliDanmakuProtocol>(ref MemoryMarshal.GetReference(span));
#else
            ref BilibiliDanmakuProtocol protocol = ref Interpret(buffer);
#endif
            protocol.PacketLength = buffer.Length;
            protocol.Action = action;
            protocol.Magic = 16;
            protocol.Parameter = 1;
            protocol.Version = 2;
            protocol.ChangeEndian();
#if NETSTANDARD2_0
            Buffer.BlockCopy(body, 0, buffer, 16, body.Length);
#elif NET5_0_OR_GREATER
            Unsafe.CopyBlock(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), 16), ref MemoryMarshal.GetArrayDataReference(body), (uint)body.Length);
#else
            Unsafe.CopyBlock(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), 16), ref MemoryMarshal.GetReference(body.AsSpan()), (uint)body.Length);
#endif
            return buffer;
        }

        protected volatile bool _Connected;

        protected CancellationTokenSource? _Cts = new CancellationTokenSource();

        protected CancellationTokenSource? _WorkerCts;

        public bool Connected => _Connected;

        public int LastConnectedRoomId { get; private set; }

        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

        public event DanmakuClientEventHandler<ConnectedEventArgs>? ConnectedEvt;

        public event DanmakuClientEventHandler<DisconnectedEventArgs>? DisconnectedEvt;

        public event DanmakuClientEventHandler<ReceivedMessageEventArgs>? ReceivedMessageEvt;

        public event DanmakuClientEventHandler<ReceivedPopularityEventArgs>? ReceivedPopularityEvt;

        private void CheckDisposed()
        {
            if (Volatile.Read(ref _Cts) == null)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        public async Task ConnectAsync(int roomId, CancellationToken token = default)
        {
            CheckDisposed();
            CancellationTokenSource? cts = Volatile.Read(ref _Cts);
            if (cts == null)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
            CancellationToken ctsToken = cts.Token;
            CancellationToken createdToken;
            CancellationTokenSource? previousWCts = Volatile.Read(ref _WorkerCts);
            CancellationTokenSource? createdWCts = null;
            if (previousWCts != null || Interlocked.CompareExchange(ref _WorkerCts, createdWCts = CancellationTokenSource.CreateLinkedTokenSource(ctsToken, token), null) != null)
            {
                createdWCts?.Dispose();
                throw new InvalidOperationException();
            }
            createdToken = createdWCts.Token;
            try
            {
                await InternalConnectAsync(roomId, createdToken).ConfigureAwait(false);
#if NET5_0_OR_GREATER
                TaskCompletionSource tcs = new TaskCompletionSource();
#else
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
#endif
                ReceiveMessageAsyncLoop(tcs, createdWCts!, createdToken);
                await tcs.Task.ConfigureAwait(false);
                _Connected = true;
                SendHeartBeatAsyncLoop(createdWCts!, createdToken);
            }
            catch
            {
                createdWCts!.Cancel();
                createdWCts.Dispose();
                Interlocked.CompareExchange(ref _WorkerCts, null!, createdWCts);
                throw;
            }
        }

        protected abstract Task InternalConnectAsync(int roomId, CancellationToken token);

        public void Disconnect()
        {
            CancellationTokenSource? workerCts = Volatile.Read(ref _WorkerCts);
            if (workerCts != null)
            {
                Disconnect(workerCts);
            }
        }

        public void Disconnect(CancellationTokenSource workerCts, Exception? e = null)
        {
            if (Interlocked.CompareExchange(ref _WorkerCts, null, workerCts) == workerCts)
            {
                int roomId = LastConnectedRoomId;
                _Connected = false;
                workerCts.Cancel();
                workerCts.Dispose();
                InternalDisconnect();
                CancellationTokenSource? cts = Volatile.Read(ref _Cts);
                CancellationToken token;
                try
                {
                    token = cts == null ? new CancellationToken(true) : cts.Token;
                }
                catch (ObjectDisposedException)
                {
                    token = new CancellationToken(true);
                }
                InvokeEvt(ref DisconnectedEvt, new DisconnectedEventArgs(roomId, e, token));
            }
        }

        protected virtual void InternalDisconnect() { }

        protected virtual void Dispose(bool disposing)
        {
            CancellationTokenSource? previousCts = Volatile.Read(ref _Cts);
            if (previousCts != null && Interlocked.CompareExchange(ref _Cts, null, previousCts) == previousCts)
            {
                try { Disconnect(); } catch { }
                previousCts.Cancel();
                previousCts.Dispose();
                InternalDispose(disposing);
            }
        }

        protected virtual void InternalDispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_0
        protected abstract Task SendAsync(ArraySegment<byte> segment, CancellationToken token);

        protected abstract Task ReceiveAsync(ArraySegment<byte> segment, CancellationToken token);
#else
        protected abstract ValueTask SendAsync(ReadOnlyMemory<byte> memory, CancellationToken token);

        protected abstract ValueTask ReceiveAsync(Memory<byte> memory, CancellationToken token);
#endif

        private async void SendHeartBeatAsyncLoop(CancellationTokenSource workerCts, CancellationToken token)
        {
            double tickFrequency = 10000 * 1000 / (double)Stopwatch.Frequency;
            long ticks;
#if NETSTANDARD2_0
            ArraySegment<byte> heartBeatPacket = new ArraySegment<byte>(DanmakuClientBase.heartBeatPacket);
#endif
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    ticks = Stopwatch.GetTimestamp();
                    await SendAsync(heartBeatPacket, token).ConfigureAwait(false);
                    TimeSpan toSleep = HeartbeatInterval - TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - ticks) * tickFrequency));
                    if (toSleep <= TimeSpan.Zero)
                    {
                        throw new TimeoutException("Heartbeat timed out.");
                    }
                    await Task.Delay(toSleep, token);
                }
                catch (Exception e)
                {
                    Disconnect(workerCts, e);
                    return;
                }
            }
        }

#if NET5_0_OR_GREATER
        private async void ReceiveMessageAsyncLoop(TaskCompletionSource tcs, CancellationTokenSource workerCts, CancellationToken token)
#else
        private async void ReceiveMessageAsyncLoop(TaskCompletionSource<int> tcs, CancellationTokenSource workerCts, CancellationToken token)
#endif
        {
            ReceiveMethodLocals locals = default;
            locals.protocolBuffer = new byte[16];
            locals.payload = new byte[4096];
            locals.decompressBuffer = Array.Empty<byte>();
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
#if NETSTANDARD2_0
                    await ReceiveAsync(new ArraySegment<byte>(locals.protocolBuffer), token).ConfigureAwait(false);
#else
                    await ReceiveAsync(locals.protocolBuffer, token).ConfigureAwait(false);
#endif
                    locals.Protocol.ChangeEndian();
                    if (locals.Protocol.PacketLength > 0)
                    {
                        if (locals.PayloadLength > 65535)
                        {
                            throw new InvalidDataException($"包长度过大:{ locals.PayloadLength}");
                        }
                        if (locals.PayloadLength > locals.payload.Length)
                        {
                            locals.payload = new byte[locals.PayloadLength];
                        }
#if NETSTANDARD2_0
                        await ReceiveAsync(new ArraySegment<byte>(locals.payload, 0, locals.PayloadLength), token);
#else
                        await ReceiveAsync(new Memory<byte>(locals.payload, 0, locals.PayloadLength), token);
#endif
                        if (locals.Protocol.Action == 8)
                        {
#if NET5_0_OR_GREATER
                            tcs.TrySetResult();
#else
                            tcs.TrySetResult(0);
#endif
                            InvokeEvt(ref ConnectedEvt, new ConnectedEventArgs(LastConnectedRoomId));
                        }
                        else
                        {
                            HandlePayload(ref locals);
                        }
                    }
                }
                catch (OperationCanceledException e)
                {
                    bool result = tcs.TrySetCanceled(token);
                    Disconnect(workerCts, result ? null : e);
                    return;
                }
                catch (Exception e)
                {
                    bool result = tcs.TrySetException(e);
                    Disconnect(workerCts, result ? null : e);
                    return;
                }
            }
        }

        protected void InvokeEvt<TEventArgs>(ref DanmakuClientEventHandler<TEventArgs>? evtRef, TEventArgs args)
        {
            DanmakuClientEventHandler<TEventArgs>? evt = evtRef;
            if (evt == null)
            {
                return;
            }
            try
            {
                Delegate[] delegates = evt.GetInvocationList();
                for (int i = 0; i < delegates.Length; i++)
                {
                    Task t = ((DanmakuClientEventHandler<TEventArgs>)delegates[i]).Invoke(this, args);
                    if (!t.IsCompleted)
                    {
                        static async Task Await(int i, Task t, Delegate[] delegates, IDanmakuClient client, TEventArgs args)
                        {
                            await t;
                            i++;
                            for (; i < delegates.Length; i++)
                            {
                                await ((DanmakuClientEventHandler<TEventArgs>)delegates[i]).Invoke(client, args);
                            }
                        }
                        _ = Await(i, t, delegates, this, args);
                        return;
                    }
                }
            }
            catch // Ignore all exceptions
            {

            }
        }

        protected virtual void HandlePayload(ref ReceiveMethodLocals locals)
        {
            ProcessDanmaku(in locals.Protocol, locals.payload);
        }

        protected void ProcessDanmaku(in BilibiliDanmakuProtocol protocol, byte[] buffer)
        {
            switch (protocol.Action)
            {
                case 3:
                    {
#if NETSTANDARD2_0
                        uint popularity;
                        unsafe
                        {
                            fixed (byte* ptr = buffer)
                            {
                                popularity = (uint)IPAddress.HostToNetworkOrder(*(int*)ptr);
                            }
                        }
#else
#if NET5_0_OR_GREATER
                        uint popularity = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetArrayDataReference(buffer));
#else
                        uint popularity = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(buffer.AsSpan()));
#endif
#if !BIGENDIAN
                        popularity = BinaryPrimitives.ReverseEndianness(popularity);
#endif
#endif
                        InvokeEvt(ref ReceivedPopularityEvt, new ReceivedPopularityEventArgs(popularity));
                        break;
                    }
                case 5:
                    {
                        try
                        {
#if NETSTANDARD2_0
                            string json = Encoding.UTF8.GetString(buffer, 0, protocol.PacketLength - 16);
                            JToken message = JToken.Parse(json);
#if false // 如果不要基于buffer创建一个string的写法
                            using MemoryStream ms = new MemoryStream(buffer, 0, protocol.PacketLength - 16);
                            using TextReader tr = new StreamReader(ms, Encoding.UTF8);
                            JToken token = (JToken)JsonSerializer.CreateDefault().Deserialize(tr, typeof(JToken))!;
#endif
#else
                            JsonElement message = JsonSerializer.Deserialize<JsonElement>(new ReadOnlySpan<byte>(buffer, 0, protocol.PacketLength - 16));
#endif
                            InvokeEvt(ref ReceivedMessageEvt, new ReceivedMessageEventArgs(message));

                        }
                        catch // Ignore all exceptions
                        {

                        }
                        break;
                    }
            }
        }

        protected byte[] CreateJoinRoomPayload(int roomId, int userId, string token)
        {
            var payload = new
            {
                uid = userId,
                roomid = roomId,
                protover = Version,
                platform = "web",
                clientver = "1.13.4",
                type = 2,
                key = token
            };
#if !NETSTANDARD2_0
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(payload);
#else
            byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
#endif
            return CreatePayload(7, json);
        }

        protected static ref BilibiliDanmakuProtocol Interpret(byte[] protocolBuffer)
        {
#if NET5_0_OR_GREATER
            return ref Unsafe.As<byte, BilibiliDanmakuProtocol>(ref MemoryMarshal.GetArrayDataReference(protocolBuffer));
#elif !NETSTANDARD2_0
            return ref Interpret(protocolBuffer.AsSpan());
#else
            unsafe // 本段代码非常的低效 (18条asm)
            {
                fixed (byte* ptr = protocolBuffer)
                {
                    return ref *(BilibiliDanmakuProtocol*)ptr;
                }
            }
#endif
        }

#if !NETSTANDARD2_0
        protected static ref BilibiliDanmakuProtocol Interpret(ReadOnlySpan<byte> protocolSpan)
        {
            return ref Unsafe.As<byte, BilibiliDanmakuProtocol>(ref MemoryMarshal.GetReference(protocolSpan));
        }
#endif

        protected struct ReceiveMethodLocals
        {
            public byte[] protocolBuffer;

            public byte[] payload;

            public byte[] decompressBuffer;

            public int PayloadLength => Protocol.PacketLength - 16;

            public ref BilibiliDanmakuProtocol Protocol => ref Interpret(protocolBuffer);
        }
    }
}

