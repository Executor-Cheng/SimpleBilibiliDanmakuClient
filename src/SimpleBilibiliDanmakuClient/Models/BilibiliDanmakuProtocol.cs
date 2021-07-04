using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NETSTANDARD2_0
using System.Net;
#else
using System.Buffers.Binary;
#endif

namespace SimpleBilibiliDanmakuClient.Models
{
    /// <summary>
    /// 表示 Bilibili 直播平台的弹幕协议头
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BilibiliDanmakuProtocol
    {
        /// <summary>
        /// 消息总长度 (协议头 + 数据长度)
        /// </summary>
        [FieldOffset(0)]
        public int PacketLength;
        /// <summary>
        /// 消息头长度 (固定为16[sizeof(DanmakuProtocol)])
        /// </summary>
        [FieldOffset(4)]
        public short Magic;
        /// <summary>
        /// 压缩标志
        /// </summary>
        [FieldOffset(4)]
        public long CompressedFlag;
        /// <summary>
        /// 消息版本号
        /// </summary>
        [FieldOffset(6)]
        public short Version;
        /// <summary>
        /// 消息类型
        /// </summary>
        [FieldOffset(8)]
        public int Action;
        /// <summary>
        /// 参数, 固定为1
        /// </summary>
        [FieldOffset(12)]
        public int Parameter;

        /// <summary>
        /// 将协议头转为网络字节序
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ChangeEndian()
        {
#if !BIGENDIAN
#if NETSTANDARD2_0
            PacketLength = IPAddress.HostToNetworkOrder(PacketLength);
            Magic = IPAddress.HostToNetworkOrder(Magic);
            Version = IPAddress.HostToNetworkOrder(Version);
            Action = IPAddress.HostToNetworkOrder(Action);
            Parameter = IPAddress.HostToNetworkOrder(Parameter);
#else
            PacketLength = BinaryPrimitives.ReverseEndianness(PacketLength);
            Magic = BinaryPrimitives.ReverseEndianness(Magic);
            Version = BinaryPrimitives.ReverseEndianness(Version);
            Action = BinaryPrimitives.ReverseEndianness(Action);
            Parameter = BinaryPrimitives.ReverseEndianness(Parameter);
#endif
#endif
        }
    }
}

