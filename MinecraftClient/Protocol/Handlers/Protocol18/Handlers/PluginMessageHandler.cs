using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class PluginMessageHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            String channel = dataTypes.ReadNextString(packetData);
            // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
            if (protocolversion < MC18Version)
                pForge.ReadNextVarShort(packetData);
            handler.OnPluginChannelMessage(channel, packetData.ToArray());
            return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
        }
    }
}
