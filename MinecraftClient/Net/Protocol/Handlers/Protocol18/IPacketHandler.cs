using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    interface IPacketHandler
    {
        bool HandlePacket(PacketIncomingType packetType, List<byte> data);
    }
}
