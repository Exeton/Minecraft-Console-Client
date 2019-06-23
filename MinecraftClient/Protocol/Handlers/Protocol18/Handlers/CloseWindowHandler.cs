using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class CloseWindowHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
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
