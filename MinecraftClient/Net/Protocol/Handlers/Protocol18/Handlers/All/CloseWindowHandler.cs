using MinecraftClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class CloseWindowHandler : IPacketHandler
    {
        IPlayer player;
        DataTypes dataTypes;
        public CloseWindowHandler(DataTypes dataTypes, IPlayer player)
        {
            this.dataTypes = dataTypes;
            this.player = player;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (player.GetInventoryEnabled())
            {
                byte windowID = dataTypes.ReadNextByte(packetData);

                player.onInventoryClose(windowID);
            }
            return true;
        }
    }
}
