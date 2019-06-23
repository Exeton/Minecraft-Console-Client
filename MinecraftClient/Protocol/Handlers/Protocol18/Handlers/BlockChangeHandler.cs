using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class BlockChangeHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (handler.GetTerrainEnabled())
            {
                if (protocolversion < MC18Version)
                {
                    int blockX = dataTypes.ReadNextInt(packetData);
                    int blockY = dataTypes.ReadNextByte(packetData);
                    int blockZ = dataTypes.ReadNextInt(packetData);
                    short blockId = (short)dataTypes.ReadNextVarInt(packetData);
                    byte blockMeta = dataTypes.ReadNextByte(packetData);
                    handler.GetWorld().SetBlock(new Location(blockX, blockY, blockZ), new Block(blockId, blockMeta));
                }
                else handler.GetWorld().SetBlock(dataTypes.ReadNextLocation(packetData), new Block((ushort)dataTypes.ReadNextVarInt(packetData)));
            }
            return true;
        }
    }
}
