using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    class Protocol18PacketSender : IPacketSender
    {

        Protocol18Handler protocol18;

        public Protocol18PacketSender(Protocol18Handler protocol18Handler)
        {
            protocol18 = protocol18Handler;
        }

        public void SendPacket(PacketOutgoingType packetOutgoingType, IEnumerable<byte> data)
        {
            protocol18.SendPacket(packetOutgoingType, data);
        }
    }
}
