using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class UnloadChunkHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (protocolversion >= MC19Version && handler.GetTerrainEnabled())
            {
                int chunkX = dataTypes.ReadNextInt(packetData);
                int chunkZ = dataTypes.ReadNextInt(packetData);
                handler.GetWorld()[chunkX, chunkZ] = null;
            }
            return true;
        }
    }
}
