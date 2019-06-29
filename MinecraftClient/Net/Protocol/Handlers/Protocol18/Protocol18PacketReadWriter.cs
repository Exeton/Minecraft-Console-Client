using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    class Protocol18PacketReadWriter : IPacketReadWriter
    {
        ConnectionInfo connectionInfo;
        DataTypes dataTypes;
        int protocolVersion;

        public Protocol18PacketReadWriter(ConnectionInfo connectionInfo, DataTypes dataTypes, int protocolVersion)
        {
            this.connectionInfo = connectionInfo;
            this.dataTypes = dataTypes;
            this.protocolVersion = protocolVersion;
        }

        public Packet ReadNext()
        {
            Packet packet = new Packet();
            int size = dataTypes.ReadNextVarIntRAW(connectionInfo.socketWrapper);
            packet.data.AddRange(connectionInfo.socketWrapper.ReadDataRAW(size));

            //Handle packet decompression
            if (protocolVersion >= (int)McVersion.V18 && connectionInfo.compressionThreshold > 0)
            {
                int sizeUncompressed = dataTypes.ReadNextVarInt(packet.data);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packet.data.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packet.data.Clear();
                    packet.data.AddRange(uncompressed);
                }
            }

            packet.id = dataTypes.ReadNextVarInt(packet.data);

            return packet;
        }

        public void WritePacket(Packet packet)
        {
            WritePacket(packet.id, packet.data);
        }

        public void WritePacket(PacketOutgoingType packetOutgoingType, IEnumerable<byte> data)
        {
            WritePacket(Protocol18PacketTypes.GetPacketOutgoingID(packetOutgoingType, protocolVersion), data);
        }

        public void WritePacket(int id, IEnumerable<byte> data)
        {
            byte[] the_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(id), data.ToArray());

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
