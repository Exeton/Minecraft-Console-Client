using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class PlayerListUpdateHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (protocolversion >= MC18Version)
            {
                int action = dataTypes.ReadNextVarInt(packetData);
                int numActions = dataTypes.ReadNextVarInt(packetData);
                for (int i = 0; i < numActions; i++)
                {
                    Guid uuid = dataTypes.ReadNextUUID(packetData);
                    switch (action)
                    {
                        case 0x00: //Player Join
                            string name = dataTypes.ReadNextString(packetData);
                            int propNum = dataTypes.ReadNextVarInt(packetData);
                            for (int p = 0; p < propNum; p++)
                            {
                                string key = dataTypes.ReadNextString(packetData);
                                string val = dataTypes.ReadNextString(packetData);
                                if (dataTypes.ReadNextBool(packetData))
                                    dataTypes.ReadNextString(packetData);
                            }
                            dataTypes.ReadNextVarInt(packetData);
                            dataTypes.ReadNextVarInt(packetData);
                            if (dataTypes.ReadNextBool(packetData))
                                dataTypes.ReadNextString(packetData);
                            handler.OnPlayerJoin(uuid, name);
                            break;
                        case 0x01: //Update gamemode
                        case 0x02: //Update latency
                            dataTypes.ReadNextVarInt(packetData);
                            break;
                        case 0x03: //Update display name
                            if (dataTypes.ReadNextBool(packetData))
                                dataTypes.ReadNextString(packetData);
                            break;
                        case 0x04: //Player Leave
                            handler.OnPlayerLeave(uuid);
                            break;
                        default:
                            //Unknown player list item type
                            break;
                    }
                }
            }
            else //MC 1.7.X does not provide UUID in tab-list updates
            {
                string name = dataTypes.ReadNextString(packetData);
                bool online = dataTypes.ReadNextBool(packetData);
                short ping = dataTypes.ReadNextShort(packetData);
                Guid FakeUUID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                if (online)
                    handler.OnPlayerJoin(FakeUUID, name);
                else handler.OnPlayerLeave(FakeUUID);
            }
            return true;
        }
    }
}
