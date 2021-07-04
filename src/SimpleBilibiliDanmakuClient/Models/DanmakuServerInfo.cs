namespace SimpleBilibiliDanmakuClient.Models
{
    public class DanmakuServerInfo
    {
        public DanmakuServerHostInfo[] Hosts { get; }

        public string Token { get; }

        public DanmakuServerInfo(DanmakuServerHostInfo[] hosts, string token)
        {
            Hosts = hosts;
            Token = token;
        }
    }

    public struct DanmakuServerHostInfo
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public int WsPort { get; set; }

        public int WssPort { get; set; }

        public DanmakuServerHostInfo(string host, int port, int wsPort, int wssPort)
        {
            Host = host;
            Port = port;
            WsPort = wsPort;
            WssPort = wssPort;
        }
    }
}
