using SimpleBilibiliDanmakuClient.Extensions;
using SimpleBilibiliDanmakuClient.Exceptions;
using SimpleBilibiliDanmakuClient.Models;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD2_0
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace SimpleBilibiliDanmakuClient.Apis
{
    public static class BiliApis
    {
        private static readonly HttpClient _client = new HttpClient();

        public static async Task<DanmakuServerInfo> GetDanmakuServerInfoAsync(int roomId, CancellationToken token = default)
        {
#if NETSTANDARD2_0
            JToken root = await _client.GetAsync($"https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={roomId}&type=0", token).GetJsonAsync(token);
            if (root["code"]!.ToObject<int>() == 0)
            {
                JToken data = root["data"]!,
                       server = data["host_list"]!;
                return new DanmakuServerInfo(server.Select(p => new DanmakuServerHostInfo(
                    p["host"]!.ToString()!,
                    p["port"]!.ToObject<int>(),
                    p["ws_port"]!.ToObject<int>(),
                    p["wss_port"]!.ToObject<int>()
                    )).ToArray(), data["token"]!.ToString());
            }
            throw new UnknownResponseException(root);
#else
            using JsonDocument j = await _client.GetAsync($"https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={roomId}&type=0", token).GetJsonAsync(token);
            JsonElement root = j.RootElement;
            if (root.GetProperty("code").GetInt32() == 0)
            {
                JsonElement data = root.GetProperty("data"),
                            server = data.GetProperty("host_list");
                return new DanmakuServerInfo(server.EnumerateArray().Select(p => new DanmakuServerHostInfo(
                    p.GetProperty("host").GetString()!,
                    p.GetProperty("port").GetInt32(),
                    p.GetProperty("ws_port").GetInt32(),
                    p.GetProperty("wss_port").GetInt32()
                    )).ToArray(), data.GetProperty("token").GetString()!);
            }
            throw new UnknownResponseException(in root);
#endif
        }
    }
}
