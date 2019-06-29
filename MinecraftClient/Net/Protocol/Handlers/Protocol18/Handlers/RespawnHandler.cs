using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class RespawnHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        WorldInfo worldInfo;
        int protocolversion;
        public RespawnHandler(IMinecraftComHandler handler, DataTypes dataTypes, WorldInfo worldInfo, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.worldInfo = worldInfo;
            this.protocolversion = protocolversion;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            worldInfo.dimension = dataTypes.ReadNextInt(packetData);
            if (protocolversion < (int)McVersion.V114)
                dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
            dataTypes.ReadNextByte(packetData);
            dataTypes.ReadNextString(packetData);
            handler.OnRespawn();
            return true;
        }
    }
}
