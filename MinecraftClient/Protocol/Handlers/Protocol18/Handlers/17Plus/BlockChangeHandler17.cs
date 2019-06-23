using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers._17Terrain
{
    class BlockChangeHandler17 : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        public BlockChangeHandler17(IMinecraftComHandler handler, DataTypes dataTypes)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            int blockX = dataTypes.ReadNextInt(packetData);
            int blockY = dataTypes.ReadNextByte(packetData);
            int blockZ = dataTypes.ReadNextInt(packetData);
            short blockId = (short)dataTypes.ReadNextVarInt(packetData);
            byte blockMeta = dataTypes.ReadNextByte(packetData);
            handler.GetWorld().SetBlock(new Location(blockX, blockY, blockZ), new Block(blockId, blockMeta));

            return true;
        }
    }
}
