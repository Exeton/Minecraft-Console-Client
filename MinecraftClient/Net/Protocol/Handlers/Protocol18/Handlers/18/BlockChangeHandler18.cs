using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class BlockChangeHandler18 : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        public BlockChangeHandler18(IMinecraftComHandler handler, DataTypes dataTypes)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            handler.GetWorld().SetBlock(dataTypes.ReadNextLocation(packetData), new Block((ushort)dataTypes.ReadNextVarInt(packetData)));         
            return true;
        }
    }
}
