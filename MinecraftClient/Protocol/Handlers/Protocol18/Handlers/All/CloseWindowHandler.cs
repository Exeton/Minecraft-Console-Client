using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class CloseWindowHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        public CloseWindowHandler(IMinecraftComHandler handler, DataTypes dataTypes)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (handler.GetInventoryEnabled())
            {
                byte windowID = dataTypes.ReadNextByte(packetData);

                handler.onInventoryClose(windowID);
            }
            return true;
        }
    }
}
