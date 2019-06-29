using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class NetworkCompressionThresholdHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        Protocol18Handler Protocol18Handler;
        int protocolversion;
        public NetworkCompressionThresholdHandler(IMinecraftComHandler handler, DataTypes dataTypes, Protocol18Handler Protocol18Handler, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.Protocol18Handler = Protocol18Handler;
            this.protocolversion = protocolversion;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (protocolversion >= (int)McVersion.V18 && protocolversion < (int)McVersion.V19)
                Protocol18Handler.connectionInfo.compressionThreshold = dataTypes.ReadNextVarInt(packetData);
            return true;
        }
    }
}
