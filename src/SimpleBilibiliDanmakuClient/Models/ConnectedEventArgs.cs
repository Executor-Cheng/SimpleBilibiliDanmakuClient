using System;

namespace SimpleBilibiliDanmakuClient.Models
{
    public class ConnectedEventArgs : EventArgs
    {
        public int RoomId { get; }

        public ConnectedEventArgs(int roomId)
        {
            RoomId = roomId;
        }
    }
}
