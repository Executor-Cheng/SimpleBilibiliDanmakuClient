using System.Threading.Tasks;

namespace SimpleBilibiliDanmakuClient.Clients
{
    public delegate Task DanmakuClientEventHandler<TEventArgs>(IDanmakuClient client, TEventArgs e);
}
