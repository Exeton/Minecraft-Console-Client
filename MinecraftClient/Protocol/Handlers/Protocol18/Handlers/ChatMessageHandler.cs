using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class ChatMessageHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            string message = dataTypes.ReadNextString(packetData);
            try
            {
                //Hide system messages or xp bar messages?
                byte messageType = dataTypes.ReadNextByte(packetData);
                if ((messageType == 1 && !Settings.DisplaySystemMessages)
                    || (messageType == 2 && !Settings.DisplayXPBarMessages))
                    break;
            }
            catch (ArgumentOutOfRangeException) { /* No message type */ }
            handler.OnTextReceived(message, true);
            return true;
        }
    }
}
