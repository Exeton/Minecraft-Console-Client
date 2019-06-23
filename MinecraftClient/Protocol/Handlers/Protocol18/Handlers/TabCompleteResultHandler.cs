using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class TabCompleteResultHandler : IPacketHandler
    {

        IMinecraftComHandler handler;
        DataTypes dataTypes;
        int protocolversion;
        Protocol18Handler protocol18Handler;

        public TabCompleteResultHandler(IMinecraftComHandler handler, DataTypes dataTypes, Protocol18Handler protocol18Handler, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.protocol18Handler = protocol18Handler;
            this.protocolversion = protocolversion;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (protocolversion >= (int)McVersion.V113)
            {
                protocol18Handler.autocomplete_transaction_id = dataTypes.ReadNextVarInt(packetData);
                dataTypes.ReadNextVarInt(packetData); // Start of text to replace
                dataTypes.ReadNextVarInt(packetData); // Length of text to replace
            }

            int autocomplete_count = dataTypes.ReadNextVarInt(packetData);
            protocol18Handler.autocomplete_result.Clear();

            for (int i = 0; i < autocomplete_count; i++)
            {
                protocol18Handler.autocomplete_result.Add(dataTypes.ReadNextString(packetData));
                if (protocolversion >= (int)McVersion.V113)
                {
                    // Skip optional tooltip for each tab-complete result
                    if (dataTypes.ReadNextBool(packetData))
                        dataTypes.ReadNextString(packetData);
                }
            }

            protocol18Handler.autocomplete_received = true;
            return true;
        }
    }
}
