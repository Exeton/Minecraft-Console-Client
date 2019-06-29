using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class KickHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        public KickHandler(IMinecraftComHandler handler, DataTypes dataTypes)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
            return false;
        }
    }
}
