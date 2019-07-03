using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class ChunkDataHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        Protocol18Terrain pTerrain;
        int protocolversion;
        WorldInfo worldInfo;

        public ChunkDataHandler(IMinecraftComHandler handler, DataTypes dataTypes, Protocol18Terrain protocol18Terrain, WorldInfo worldInfo, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.pTerrain = protocol18Terrain;
            this.worldInfo = worldInfo;
            this.protocolversion = protocolversion;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            int chunkX = dataTypes.ReadNextInt(packetData);
            int chunkZ = dataTypes.ReadNextInt(packetData);
            bool chunksContinuous = dataTypes.ReadNextBool(packetData);
            ushort chunkMask = protocolversion >= (int)McVersion.V19
                ? (ushort)dataTypes.ReadNextVarInt(packetData)
                : dataTypes.ReadNextUShort(packetData);
            if (protocolversion <  (int)McVersion.V18)
            {
                ushort addBitmap = dataTypes.ReadNextUShort(packetData);
                int compressedDataSize = dataTypes.ReadNextInt(packetData);
                byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                byte[] decompressed = ZlibUtils.Decompress(compressed);
                pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, worldInfo.dimension == 0, chunksContinuous, worldInfo.dimension, new List<byte>(decompressed));
            }
            else
            {
                if (protocolversion >= (int)McVersion.V114)
                    dataTypes.ReadNextNbt(packetData);  // Heightmaps - 1.14 and above
                int dataSize = dataTypes.ReadNextVarInt(packetData);
                pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, worldInfo.dimension, packetData);
            }
            
            return true;
        }
    }
}
