using SimpleBilibiliDanmakuClient.Clients;
using SimpleBilibiliDanmakuClient.Models;
using System;
using System.Threading.Tasks;

namespace SimpleBilibiliDanmakuClient.Example.cs
{
    public static class Program
    {
        public static async Task Main()
        {
            TcpDanmakuClientV2 client = new TcpDanmakuClientV2();
            await client.ConnectAsync(5441);
            client.ReceivedPopularityEvt += Client_ReceivedPopularityEvt;
            client.ReceivedMessageHandlerEvt += Client_ReceivedMessageHandlerEvt;
            await Task.Delay(-1);
        }

        private static Task Client_ReceivedPopularityEvt(IDanmakuClient client, ReceivedPopularityEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 当前房间人气:{e.Popularity}");
            return Task.CompletedTask;
        }

        private static Task Client_ReceivedMessageHandlerEvt(IDanmakuClient client, ReceivedMessageEventArgs e)
        {
#if NETFRAMEWORK
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {e.Message.ToString(0)}");
#else
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {e.Message.GetRawText()}");
#endif
            return Task.CompletedTask;
        }
    }
}
