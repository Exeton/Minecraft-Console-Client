using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    class Protocol18PacketSender : IPacketSender
    {
        DataTypes dataTypes;
        ConnectionInfo connectionInfo;
        int protocolVersion;

        public Protocol18PacketSender(DataTypes dataTypes, ConnectionInfo connectionInfo, int protocolVersion)
        {
            this.dataTypes = dataTypes;
            this.connectionInfo = connectionInfo;
            this.protocolVersion = protocolVersion;
        }

        public void SendPacket(PacketOutgoingType packetOutgoingType, IEnumerable<byte> data)
        {
            SendPacket(Protocol18PacketTypes.GetPacketOutgoingID(packetOutgoingType, protocolVersion), data);
        }

        public void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            byte[] the_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(packetID), packetData.ToArray());

            if (connectionInfo.compressionThreshold > 0) //Compression enabled?
            {
                if (the_packet.Length >= connectionInfo.compressionThreshold) //Packet long enough for compressing?
                {
                    byte[] compressed_packet = ZlibUtils.Compress(the_packet);
                    the_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), compressed_packet);
                }
                else
                {
                    byte[] uncompressed_length = dataTypes.GetVarInt(0); //Not compressed (short packet)
                    the_packet = dataTypes.ConcatBytes(uncompressed_length, the_packet);
                }
            }

            connectionInfo.socketWrapper.SendDataRAW(dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), the_packet));
        }
    }
}
