#if !NETSTANDARD2_0
using SimpleBilibiliDanmakuClient.Models;
using System;
using System.Buffers;
using System.IO.Compression;

namespace SimpleBilibiliDanmakuClient.Clients
{
    public class WsDanmakuClientV3 : WsDanmakuClientBase
    {
        protected override byte Version => 3;

        protected override void HandlePayload(ref ReceiveMethodLocals locals)
        {
#if BIGENDIAN
            const long gzipCompressed = 0x0500000003001000L; // Magic = 0x10; Version = 0x2; Action = 0x5, cmp once
#else
            const long gzipCompressed = 0x0000000500030010L;
#endif
            // Skip PacketLength (4 bytes)
            if (locals.Protocol.CompressedFlag == gzipCompressed)
            {
                ProcessBrPayload(locals.PayloadLength, locals.protocolBuffer, locals.payload, ref locals.decompressBuffer);
            }
            else
            {
                base.HandlePayload(ref locals);
            }
        }

        private void ProcessBrPayload(int payloadLength, byte[] protocolBuffer, byte[] payload, ref byte[] decompressBuffer)
        {
            using BrotliDecoder decoder = default;
            Span<byte> protocolSpan = protocolBuffer;
            Span<byte> decompressSpan = decompressBuffer;
            ReadOnlySpan<byte> payloadSpan = payload.AsSpan(0, payloadLength);
            ref BilibiliDanmakuProtocol protocol = ref Interpret(protocolSpan);
            while (true)
            {
                decoder.Decompress(payloadSpan, protocolSpan, out int consumed, out _);
                payloadSpan = payloadSpan[consumed..];
                protocol.ChangeEndian();
                payloadLength = protocol.PacketLength - 16;
                if (decompressSpan.Length < payloadLength)
                {
                    decompressBuffer = new byte[payloadLength];
                    decompressSpan = decompressBuffer;
                }
                OperationStatus status = decoder.Decompress(payloadSpan, decompressSpan[..payloadLength], out consumed, out _);
                ProcessDanmaku(in protocol, decompressBuffer);
                if (status == OperationStatus.Done)
                {
                    break;
                }
                payloadSpan = payloadSpan[consumed..];
            }
        }
    }
}
#endif
