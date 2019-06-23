using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class ResourecePackSendHandler : IPacketHandler
    {

        int protocolversion;
        DataTypes dataTypes;
        IPacketReadWriter packetSender;

        public ResourecePackSendHandler(DataTypes dataTypes, IPacketReadWriter packetSender, int protocolversion)
        {
            this.dataTypes = dataTypes;
            this.packetSender = packetSender;
            this.protocolversion = protocolversion;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            string url = dataTypes.ReadNextString(packetData);
            string hash = dataTypes.ReadNextString(packetData);
            //Send back "accepted" and "successfully loaded" responses for plugins making use of resource pack mandatory
            byte[] responseHeader = new byte[0];
            if (protocolversion < (int)McVersion.V110) //MC 1.10 does not include resource pack hash in responses
                responseHeader = dataTypes.ConcatBytes(dataTypes.GetVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
            packetSender.WritePacket(PacketOutgoingType.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(3))); //Accepted pack
            packetSender.WritePacket(PacketOutgoingType.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(0))); //Successfully loaded
            return true;
        }
    }
}
