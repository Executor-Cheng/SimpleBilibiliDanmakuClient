using System;

namespace SimpleBilibiliDanmakuClient.Models
{
    public class ReceivedMessageEventArgs : EventArgs
    {
#if NETSTANDARD2_0
        public Newtonsoft.Json.Linq.JToken Message { get; }

        public ReceivedMessageEventArgs(Newtonsoft.Json.Linq.JToken message)
        {
            Message = message;
        }
#else
        public System.Text.Json.JsonElement Message { get; }

        public ReceivedMessageEventArgs(System.Text.Json.JsonElement message)
        {
            Message = message;
        }
#endif
    }
}
