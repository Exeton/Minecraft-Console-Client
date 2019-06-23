using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class NetworkCompressionThresholdHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (protocolversion >= MC18Version && protocolversion < MC19Version)
                compression_treshold = dataTypes.ReadNextVarInt(packetData);
            return true;
        }
    }
}
