using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class WindowItemsHandler : IPacketHandler
    {

        IMinecraftComHandler handler;
        DataTypes dataTypes;
        public WindowItemsHandler(IMinecraftComHandler handler, DataTypes dataTypes)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (handler.GetInventoryEnabled())
            {
                byte id = dataTypes.ReadNextByte(packetData);
                short elements = dataTypes.ReadNextShort(packetData);

                for (int i = 0; i < elements; i++)
                {
                    short itemID = dataTypes.ReadNextShort(packetData);
                    if (itemID == -1) continue;
                    byte itemCount = dataTypes.ReadNextByte(packetData);
                    short itemDamage = dataTypes.ReadNextShort(packetData);
                    Item item = new Item(itemID, itemCount, itemDamage, 0);
                    //TODO: Add to the dictionary for the inventory its in using the id
                    if (packetData.ToArray().Count() > 0)
                    {
                        dataTypes.ReadNextNbt(packetData);
                    }
                }
            }

            return true;
        }
    }
}
