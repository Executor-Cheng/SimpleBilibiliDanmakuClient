using SimpleBilibiliDanmakuClient.Apis;
using SimpleBilibiliDanmakuClient.Extensions;
using SimpleBilibiliDanmakuClient.Models;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleBilibiliDanmakuClient.Clients
{
    public abstract class WsDanmakuClientBase : DanmakuClientBase
    {
        protected volatile WebSocket? _Client;

        protected override async Task InternalConnectAsync(int roomId, CancellationToken token)
        {
            DanmakuServerInfo server = await BiliApis.GetDanmakuServerInfoAsync(roomId, token).ConfigureAwait(false);
            ClientWebSocket client = new ClientWebSocket();
            client.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
            token.Register(client.Dispose);
            DanmakuServerHostInfo serverHost = server.Hosts[(int)(Stopwatch.GetTimestamp() % server.Hosts.Length)];
            await client.ConnectAsync(new Uri($"wss://{serverHost.Host}:{serverHost.WssPort}/sub"), token).ConfigureAwait(false);
            await SendJoinRoomAsync(client, roomId, 0, server.Token, token).ConfigureAwait(false);
            _Client = client;
        }

#if NETSTANDARD2_0
        protected override Task SendAsync(ArraySegment<byte> memory, CancellationToken token)
        {
            WebSocket? client;
            if (token.IsCancellationRequested || (client = _Client) == null)
            {
                return Task.FromCanceled(token);
            }
            return client.SendAsync(memory, WebSocketMessageType.Binary, true, token);
        }

        protected override Task ReceiveAsync(ArraySegment<byte> memory, CancellationToken token)
        {
            WebSocket? client;
            if (token.IsCancellationRequested || (client = _Client) == null)
            {
                return Task.FromCanceled(token);
            }
            return client.ReceiveFullyAsync(memory, token);
        }

        protected Task SendJoinRoomAsync(WebSocket client, int roomId, int userId, string token, CancellationToken cToken = default)
        {
            return client.SendAsync(new ArraySegment<byte>(CreateJoinRoomPayload(roomId, userId, token)), WebSocketMessageType.Binary, true, cToken);
        }
#else
        protected override ValueTask SendAsync(ReadOnlyMemory<byte> memory, CancellationToken token)
        {
            WebSocket? client;
            if (token.IsCancellationRequested || (client = _Client) == null)
            {
                return new ValueTask(Task.FromCanceled(token));
            }
            return client.SendAsync(memory, WebSocketMessageType.Binary, true, token);
        }

        protected override ValueTask ReceiveAsync(Memory<byte> memory, CancellationToken token)
        {
            WebSocket? client;
            if (token.IsCancellationRequested || (client = _Client) == null)
            {
                return new ValueTask(Task.FromCanceled(token));
            }
            return client.ReceiveFullyAsync(memory, token);
        }

        protected ValueTask SendJoinRoomAsync(WebSocket client, int roomId, int userId, string token, CancellationToken cToken = default)
        {
            return client.SendAsync(new ReadOnlyMemory<byte>(CreateJoinRoomPayload(roomId, userId, token)), WebSocketMessageType.Binary, true, cToken);
        }
#endif
    }
}
