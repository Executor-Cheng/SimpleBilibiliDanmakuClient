using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleBilibiliDanmakuClient.Extensions
{
    public static class WebSocketExtensions
    {
#if NETSTANDARD2_0
        public static async Task ReceiveFullyAsync(this WebSocket ws, ArraySegment<byte> segment, CancellationToken token = default)
        {
            while (true)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(segment, token);
                if (result.Count == segment.Count)
                {
                    return;
                }
                if (result.EndOfMessage)
                {
                    throw new EndOfStreamException();
                }
                segment = new ArraySegment<byte>(segment.Array, segment.Offset + result.Count, segment.Count - result.Count);
            }
        }

        public static async Task<byte[]> ReceiveFullyAsync(this WebSocket webSocket, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            byte[] buffer = new byte[1024];
            using MemoryStream ms = new MemoryStream(1024);
            WebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);
            ms.Write(buffer, 0, result.Count);
            return ms.ToArray();
        }
#else
        public static async ValueTask ReceiveFullyAsync(this WebSocket ws, Memory<byte> buffer, CancellationToken token = default)
        {
            while (true)
            {
                ValueWebSocketReceiveResult result = await ws.ReceiveAsync(buffer, token);
                if (result.Count == buffer.Length)
                {
                    return;
                }
                if (result.EndOfMessage)
                {
                    throw new EndOfStreamException();
                }
                buffer = buffer[result.Count..];
            }
        }

        public static async ValueTask<byte[]> ReceiveFullyAsync(this WebSocket webSocket, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            byte[] buffer = new byte[1024];
            using MemoryStream ms = new MemoryStream(1024);
            ValueWebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(buffer.AsMemory(), token);
                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);
            ms.Write(buffer, 0, result.Count);
            return ms.ToArray();
        }
#endif
    }
}
