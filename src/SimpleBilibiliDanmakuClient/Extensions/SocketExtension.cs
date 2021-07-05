using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleBilibiliDanmakuClient.Extensions
{
    public static class SocketExtension
    {
#if !NETSTANDARD2_0
        public static ValueTask ReceiveFullyAsync(this Socket socket, byte[] buffer, CancellationToken token = default)
            => socket.ReceiveFullyAsync(new Memory<byte>(buffer, 0, buffer.Length), token);

        public static ValueTask ReceiveFullyAsync(this Socket socket, byte[] buffer, int offset, int size, CancellationToken token = default)
        {
            if (offset + size > buffer.Length)
            {
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
            }
            Memory<byte> memory = new Memory<byte>(buffer, offset, size);
            return socket.ReceiveFullyAsync(memory, token);
        }

        public static async ValueTask ReceiveFullyAsync(this Socket socket, Memory<byte> memory, CancellationToken token = default)
        {
            while (true)
            {
                int n = await socket.ReceiveAsync(memory, SocketFlags.None, token).ConfigureAwait(false);
                if (n < 1)
                {
                    throw new SocketException(10054);
                }
                else if (n < memory.Length)
                {
                    memory = memory[n..];
                }
                else
                {
                    return;
                }
            }
        }
#else
        public static Task ReceiveFullyAsync(this Socket socket, byte[] buffer, CancellationToken token = default)
            => socket.ReceiveFullyAsync(buffer, 0, buffer.Length, token);

        public static async Task ReceiveFullyAsync(this Socket socket, byte[] buffer, int offset, int size, CancellationToken token = default)
        {
            if (offset + size > buffer.Length)
            {
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
            }
            while (true)
            {
                int n = await Task.Factory.FromAsync(socket.BeginReceive(buffer, offset, size, SocketFlags.None, null, null), socket.EndReceive).ConfigureAwait(false);
                if (n < 1)
                {
                    throw new SocketException(10054);
                }
                else if (n < size)
                {
                    offset += n;
                    size -= n;
                }
                else
                {
                    return;
                }
            }
        }

        public static Task<int> SendAsync(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            return Task.Factory.FromAsync(socket.BeginSend(buffer, offset, size, socketFlags, null, null), socket.EndSend);
        }
#endif
    }
}
