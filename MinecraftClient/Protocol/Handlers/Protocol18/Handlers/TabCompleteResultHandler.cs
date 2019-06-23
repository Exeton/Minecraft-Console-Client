using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class TabCompleteResultHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (protocolversion >= MC113Version)
            {
                autocomplete_transaction_id = dataTypes.ReadNextVarInt(packetData);
                dataTypes.ReadNextVarInt(packetData); // Start of text to replace
                dataTypes.ReadNextVarInt(packetData); // Length of text to replace
            }

            int autocomplete_count = dataTypes.ReadNextVarInt(packetData);
            autocomplete_result.Clear();

            for (int i = 0; i < autocomplete_count; i++)
            {
                autocomplete_result.Add(dataTypes.ReadNextString(packetData));
                if (protocolversion >= MC113Version)
                {
                    // Skip optional tooltip for each tab-complete result
                    if (dataTypes.ReadNextBool(packetData))
                        dataTypes.ReadNextString(packetData);
                }
            }

            autocomplete_received = true;
            return true;
        }
    }
}
