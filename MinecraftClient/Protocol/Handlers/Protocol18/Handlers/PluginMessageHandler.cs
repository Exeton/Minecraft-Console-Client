using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class PluginMessageHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        Protocol18Forge pForge;
        WorldInfo worldInfo;
        int protocolversion;
        public PluginMessageHandler(IMinecraftComHandler handler, DataTypes dataTypes, Protocol18Forge pForge, WorldInfo worldInfo, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.pForge = pForge;
            this.worldInfo = worldInfo;
            this.protocolversion = protocolversion;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            String channel = dataTypes.ReadNextString(packetData);
            // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
            if (protocolversion < (int)McVersion.V18)
                pForge.ReadNextVarShort(packetData);
            handler.OnPluginChannelMessage(channel, packetData.ToArray());
            return pForge.HandlePluginMessage(channel, packetData, ref worldInfo.dimension);
        }
    }
}
