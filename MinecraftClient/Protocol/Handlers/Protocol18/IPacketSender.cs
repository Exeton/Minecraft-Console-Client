using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    interface IPacketSender
    {
        void SendPacket(PacketOutgoingType packetOutgoingType, List<byte> data);
    }
}
