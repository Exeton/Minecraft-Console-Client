using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class ChatMessageHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        public ChatMessageHandler(IMinecraftComHandler handler, DataTypes dataTypes)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            string message = dataTypes.ReadNextString(data);
            try
            {
                //Hide system messages or xp bar messages?
                byte messageType = dataTypes.ReadNextByte(data);
                if ((messageType == 1 && !Settings.DisplaySystemMessages) || (messageType == 2 && !Settings.DisplayXPBarMessages))
                    return true;
            }
            catch (ArgumentOutOfRangeException) { /* No message type */ }
            handler.OnTextReceived(message, true);
            return true;
        }
    }
}
