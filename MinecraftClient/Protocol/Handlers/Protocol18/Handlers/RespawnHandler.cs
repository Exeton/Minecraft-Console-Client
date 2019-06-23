using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class RespawnHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            this.currentDimension = dataTypes.ReadNextInt(packetData);
            if (protocolversion < MC114Version)
                dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
            dataTypes.ReadNextByte(packetData);
            dataTypes.ReadNextString(packetData);
            handler.OnRespawn();
            return true;
        }
    }
}
