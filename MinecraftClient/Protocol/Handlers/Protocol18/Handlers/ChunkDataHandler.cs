using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class ChunkDataHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (handler.GetTerrainEnabled())
            {
                int chunkX = dataTypes.ReadNextInt(packetData);
                int chunkZ = dataTypes.ReadNextInt(packetData);
                bool chunksContinuous = dataTypes.ReadNextBool(packetData);
                ushort chunkMask = protocolversion >= MC19Version
                    ? (ushort)dataTypes.ReadNextVarInt(packetData)
                    : dataTypes.ReadNextUShort(packetData);
                if (protocolversion < MC18Version)
                {
                    ushort addBitmap = dataTypes.ReadNextUShort(packetData);
                    int compressedDataSize = dataTypes.ReadNextInt(packetData);
                    byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                    byte[] decompressed = ZlibUtils.Decompress(compressed);
                    pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, currentDimension == 0, chunksContinuous, currentDimension, new List<byte>(decompressed));
                }
                else
                {
                    if (protocolversion >= MC114Version)
                        dataTypes.ReadNextNbt(packetData);  // Heightmaps - 1.14 and above
                    int dataSize = dataTypes.ReadNextVarInt(packetData);
                    pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, currentDimension, packetData);
                }
            }
            return true;
        }
    }
}
