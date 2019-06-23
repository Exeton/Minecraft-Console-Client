using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class OpenWindowHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (handler.GetInventoryEnabled())
            {
                byte windowID = dataTypes.ReadNextByte(packetData);
                string type = dataTypes.ReadNextString(packetData).Replace("minecraft:", "").ToUpper();
                InventoryType inventoryType = (InventoryType)Enum.Parse(typeof(InventoryType), type);
                string title = dataTypes.ReadNextString(packetData);
                byte slots = dataTypes.ReadNextByte(packetData);
                Inventory inventory = new Inventory(windowID, inventoryType, title, slots);

                handler.onInventoryOpen(inventory);
            }
            return true;
        }
    }
}
