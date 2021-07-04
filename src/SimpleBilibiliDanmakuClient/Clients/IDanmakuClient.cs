using SimpleBilibiliDanmakuClient.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleBilibiliDanmakuClient.Clients
{
    public interface IDanmakuClient : IDisposable
    {
        event DanmakuClientEventHandler<ConnectedEventArgs>? ConnectedEvt;

        event DanmakuClientEventHandler<DisconnectedEventArgs>? DisconnectedEvt;

        event DanmakuClientEventHandler<ReceivedMessageEventArgs>? ReceivedMessageHandlerEvt;

        event DanmakuClientEventHandler<ReceivedPopularityEventArgs>? ReceivedPopularityEvt;

        bool Connected { get; }

        TimeSpan HeartbeatInterval { get; set; }

        int LastConnectedRoomId { get; }

        Task ConnectAsync(int roomId, CancellationToken token = default);

        void Disconnect();
    }
}
