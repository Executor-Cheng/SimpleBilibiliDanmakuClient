using SimpleBilibiliDanmakuClient.Models;
using System.IO;
using System.IO.Compression;

namespace SimpleBilibiliDanmakuClient.Clients
{
    public class WsDanmakuClientV2 : WsDanmakuClientBase
    {
        protected override byte Version => 2;

        protected override void HandlePayload(ref ReceiveMethodLocals locals)
        {
#if BIGENDIAN
            const long gzipCompressed = 0x0500000002001000L; // Magic = 0x10; Version = 0x2; Action = 0x5, cmp once
#else
            const long gzipCompressed = 0x0000000500020010L;
#endif
            // Skip PacketLength (4 bytes)
            if (locals.Protocol.CompressedFlag == gzipCompressed)
            {
                ProcessGZipPayload(locals.PayloadLength, locals.protocolBuffer, locals.payload, ref locals.decompressBuffer);
            }
            else
            {
                base.HandlePayload(ref locals);
            }
        }

        private void ProcessGZipPayload(int payloadLength, byte[] protocolBuffer, byte[] payload, ref byte[] decompressBuffer)
        {
            using MemoryStream ms = new MemoryStream(payload, 2, payloadLength - 2); // skip 0x78 0xDA
            using DeflateStream deflate = new DeflateStream(ms, CompressionMode.Decompress);
            ref BilibiliDanmakuProtocol protocol = ref Interpret(protocolBuffer);
            while (deflate.Read(protocolBuffer, 0, 16) > 0)
            {
                protocol.ChangeEndian();
                payloadLength = protocol.PacketLength - 16;
                if (decompressBuffer.Length < payloadLength)
                {
                    decompressBuffer = new byte[payloadLength];
                }
                deflate.Read(decompressBuffer, 0, payloadLength);
                base.ProcessDanmaku(in protocol, decompressBuffer);
            }
        }
    }
}
