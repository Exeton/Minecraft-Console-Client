using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class MultiBlockChangeHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        int protocolversion;
        public MultiBlockChangeHandler(IMinecraftComHandler handler, DataTypes dataTypes, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.protocolversion = protocolversion;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (handler.GetTerrainEnabled())
            {
                int chunkX = dataTypes.ReadNextInt(packetData);
                int chunkZ = dataTypes.ReadNextInt(packetData);
                int recordCount = protocolversion < (int)McVersion.V18
                    ? (int)dataTypes.ReadNextShort(packetData)
                    : dataTypes.ReadNextVarInt(packetData);

                for (int i = 0; i < recordCount; i++)
                {
                    byte locationXZ;
                    ushort blockIdMeta;
                    int blockY;

                    if (protocolversion < (int)McVersion.V18)
                    {
                        blockIdMeta = dataTypes.ReadNextUShort(packetData);
                        blockY = (ushort)dataTypes.ReadNextByte(packetData);
                        locationXZ = dataTypes.ReadNextByte(packetData);
                    }
                    else
                    {
                        locationXZ = dataTypes.ReadNextByte(packetData);
                        blockY = (ushort)dataTypes.ReadNextByte(packetData);
                        blockIdMeta = (ushort)dataTypes.ReadNextVarInt(packetData);
                    }

                    int blockX = locationXZ >> 4;
                    int blockZ = locationXZ & 0x0F;
                    Block block = new Block(blockIdMeta);
                    handler.GetWorld().SetBlock(new Location(chunkX, chunkZ, blockX, blockY, blockZ), block);
                }
            }
            return true;
        }
    }
}
