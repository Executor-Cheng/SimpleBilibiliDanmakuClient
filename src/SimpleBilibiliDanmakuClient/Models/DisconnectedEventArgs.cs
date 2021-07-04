using System;
using System.Threading;

namespace SimpleBilibiliDanmakuClient.Models
{
    public class DisconnectedEventArgs : EventArgs
    {
        public int RoomId { get; }

        public Exception? Exception { get; }

        public CancellationToken Token { get; }

        public DisconnectedEventArgs(int roomId, Exception? exception, CancellationToken token)
        {
            RoomId = roomId;
            Exception = exception;
            Token = token;
        }
    }
}
