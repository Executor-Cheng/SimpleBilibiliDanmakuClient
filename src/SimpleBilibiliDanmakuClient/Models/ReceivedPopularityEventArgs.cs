using System;

namespace SimpleBilibiliDanmakuClient.Models
{
    public class ReceivedPopularityEventArgs : EventArgs
    {
        public ReceivedPopularityEventArgs(uint popularity)
        {
            Popularity = popularity;
        }

        public uint Popularity { get; }
    }
}
