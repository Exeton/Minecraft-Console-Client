using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    public interface IPacketSender
    {
        void SendPacket(PacketOutgoingType packetOutgoingType, IEnumerable<byte> data);
        void SendPacket(int packetID, IEnumerable<byte> data);
    }
}
