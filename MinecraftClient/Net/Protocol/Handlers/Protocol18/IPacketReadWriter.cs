using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    public interface IPacketReadWriter
    {
        Packet ReadNext();

        void WritePacket(PacketOutgoingType packetOutgoingType, IEnumerable<byte> data);
        void WritePacket(int id, IEnumerable<byte> data);
        void WritePacket(Packet packet);

    }
}
