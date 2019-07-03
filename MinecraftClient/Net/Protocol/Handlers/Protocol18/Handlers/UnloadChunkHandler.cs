using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class UnloadChunkHandler : IPacketHandler
    {

        IMinecraftComHandler handler;
        DataTypes dataTypes;
        int protocolversion;

        public UnloadChunkHandler(IMinecraftComHandler handler, DataTypes dataTypes, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.protocolversion = protocolversion;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (protocolversion >= (int)McVersion.V19)
            {
                int chunkX = dataTypes.ReadNextInt(packetData);
                int chunkZ = dataTypes.ReadNextInt(packetData);
                handler.GetWorld()[chunkX, chunkZ] = null;
            }
            return true;
        }
    }
}
