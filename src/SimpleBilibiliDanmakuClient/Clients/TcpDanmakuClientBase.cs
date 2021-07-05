using SimpleBilibiliDanmakuClient.Apis;
using SimpleBilibiliDanmakuClient.Extensions;
using SimpleBilibiliDanmakuClient.Models;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleBilibiliDanmakuClient.Clients
{
    public abstract class TcpDanmakuClientBase : DanmakuClientBase
    {
        protected volatile Socket? _Socket;

#if NETSTANDARD2_0
        protected override Task SendAsync(ArraySegment<byte> segment, CancellationToken token)
        {
            Socket? socket;
            if (token.IsCancellationRequested || (socket = _Socket) == null)
            {
                return Task.FromCanceled(token);
            }
            return socket.SendAsync(segment.Array, segment.Offset, segment.Count, SocketFlags.None);
        }

        protected override Task ReceiveAsync(ArraySegment<byte> segment, CancellationToken token)
        {
            Socket? socket;
            if (token.IsCancellationRequested || (socket = _Socket) == null)
            {
                return Task.FromCanceled(token);
            }
            return socket.ReceiveFullyAsync(segment.Array, segment.Offset, segment.Count, token);
        }
#else
        protected override ValueTask SendAsync(ReadOnlyMemory<byte> memory, CancellationToken token)
        {
            Socket? socket;
            if (token.IsCancellationRequested || (socket = _Socket) == null)
            {
                return new ValueTask(Task.FromCanceled(token));
            }
            return new ValueTask(socket.SendAsync(memory, SocketFlags.None, token).AsTask());
        }

        protected override ValueTask ReceiveAsync(Memory<byte> memory, CancellationToken token)
        {
            Socket? socket;
            if (token.IsCancellationRequested || (socket = _Socket) == null)
            {
                return new ValueTask(Task.FromCanceled(token));
            }
            return socket.ReceiveFullyAsync(memory, token);
        }
#endif

        protected override async Task InternalConnectAsync(int roomId, CancellationToken token)
        {
            DanmakuServerInfo server = await BiliApis.GetDanmakuServerInfoAsync(roomId, token).ConfigureAwait(false);
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = socket.ReceiveTimeout = (int)HeartbeatInterval.TotalMilliseconds + 10000;
            token.Register(socket.Dispose);
            DanmakuServerHostInfo serverHost = server.Hosts[(int)(Stopwatch.GetTimestamp() % server.Hosts.Length)];
#if NET5_0_OR_GREATER
            await socket.ConnectAsync(serverHost.Host, serverHost.Port, token).ConfigureAwait(false);
#else
            await socket.ConnectAsync(serverHost.Host, serverHost.Port).ConfigureAwait(false);
#endif
#if NETSTANDARD2_0
            await SendJoinRoomAsync(socket, roomId, 0, server.Token).ConfigureAwait(false);
#else
            await SendJoinRoomAsync(socket, roomId, 0, server.Token, token).ConfigureAwait(false);
#endif
            _Socket = socket;
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task SendJoinRoomAsync(Socket socket, int roomId, int userId, string token)
        {
            byte[] payload = CreateJoinRoomPayload(roomId, userId, token);
            return socket.SendAsync(payload, 0, payload.Length, SocketFlags.None);
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ValueTask SendJoinRoomAsync(Socket socket, int roomId, int userId, string token, CancellationToken cToken = default)
        {
            return new ValueTask(socket.SendAsync(CreateJoinRoomPayload(roomId, userId, token), SocketFlags.None, cToken).AsTask());
        }
#endif
    }
}
