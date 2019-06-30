using MinecraftClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class OpenWindowHandler : IPacketHandler
    {
        Player player;
        DataTypes dataTypes;
        public OpenWindowHandler(Player player, DataTypes dataTypes)
        {
            this.player = player;
            this.dataTypes = dataTypes;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (player.GetInventoryEnabled())
            {
                byte windowID = dataTypes.ReadNextByte(packetData);
                string type = dataTypes.ReadNextString(packetData).Replace("minecraft:", "").ToUpper();
                InventoryType inventoryType = (InventoryType)Enum.Parse(typeof(InventoryType), type);
                string title = dataTypes.ReadNextString(packetData);
                byte slots = dataTypes.ReadNextByte(packetData);
                Inventory inventory = new Inventory(windowID, inventoryType, title, slots);

                player.onInventoryOpen(inventory);
            }
            return true;
        }
    }
}
