using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class KickHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
            return false;
        }
    }
}
