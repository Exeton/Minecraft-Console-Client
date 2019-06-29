using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class KeepAliveHandler : IPacketHandler
    {
        IPacketReadWriter packetSender;
        public KeepAliveHandler(IPacketReadWriter packetSender)
        {
            this.packetSender = packetSender;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            packetSender.WritePacket(PacketOutgoingType.KeepAlive, data);
            return true;
        }
    }
}
