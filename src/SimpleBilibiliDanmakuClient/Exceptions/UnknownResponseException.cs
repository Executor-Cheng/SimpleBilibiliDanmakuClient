using System;

namespace SimpleBilibiliDanmakuClient.Exceptions
{
    public sealed class UnknownResponseException : Exception
    {
        public string? Response { get; }

        public UnknownResponseException() { }

#if NETSTANDARD2_0
        public UnknownResponseException(Newtonsoft.Json.Linq.JToken root) : this(root.ToString(0)) { }

        public UnknownResponseException(Newtonsoft.Json.Linq.JToken root, string message) : this(root.ToString(0), message) { }
#else
        public UnknownResponseException(in System.Text.Json.JsonElement root) : this(root.GetRawText()) { }

        public UnknownResponseException(in System.Text.Json.JsonElement root, string message) : this(root.GetRawText(), message) { }
#endif
        public UnknownResponseException(string response) : this(response, "未知的服务器返回.") { }

        public UnknownResponseException(string response, string message) : base(message)
            => Response = response;

        public UnknownResponseException(string message, Exception innerException) : base(message, innerException) { }

        public UnknownResponseException(string response, string message, Exception innerException) : base(message, innerException) => Response = response;

        public override string ToString()
        {
            return base.ToString() + "\r\n" + Response;
        }
    }
}
