using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class MapChunkBulkHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        int protocolversion;
        Protocol18Terrain pTerrain;
        WorldInfo worldInfo;
        public MapChunkBulkHandler(IMinecraftComHandler handler, DataTypes dataTypes, int protocolversion, Protocol18Terrain pTerrain, WorldInfo worldInfo)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.protocolversion = protocolversion;
            this.pTerrain = pTerrain;
            this.worldInfo = worldInfo;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            if (protocolversion < (int)McVersion.V19 && handler.GetTerrainEnabled())
            {
                int chunkCount;
                bool hasSkyLight;
                List<byte> chunkData = packetData;

                //Read global fields
                if (protocolversion < (int)McVersion.V18)
                {
                    chunkCount = dataTypes.ReadNextShort(packetData);
                    int compressedDataSize = dataTypes.ReadNextInt(packetData);
                    hasSkyLight = dataTypes.ReadNextBool(packetData);
                    byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                    byte[] decompressed = ZlibUtils.Decompress(compressed);
                    chunkData = new List<byte>(decompressed);
                }
                else
                {
                    hasSkyLight = dataTypes.ReadNextBool(packetData);
                    chunkCount = dataTypes.ReadNextVarInt(packetData);
                }

                //Read chunk records
                int[] chunkXs = new int[chunkCount];
                int[] chunkZs = new int[chunkCount];
                ushort[] chunkMasks = new ushort[chunkCount];
                ushort[] addBitmaps = new ushort[chunkCount];
                for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                {
                    chunkXs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                    chunkZs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                    chunkMasks[chunkColumnNo] = dataTypes.ReadNextUShort(packetData);
                    addBitmaps[chunkColumnNo] = protocolversion < (int)McVersion.V18
                        ? dataTypes.ReadNextUShort(packetData)
                        : (ushort)0;
                }

                //Process chunk records
                for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                    pTerrain.ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo], chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true, worldInfo.dimension, chunkData);
            }
            return true;
        }
    }
}
