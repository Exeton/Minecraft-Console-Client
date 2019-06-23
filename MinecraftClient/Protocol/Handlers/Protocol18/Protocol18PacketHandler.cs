using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.Protocol18.Handlers;
using MinecraftClient.Protocol.Handlers.Protocol18.Handlers._17Terrain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    class Protocol18PacketHandler : IPacketHandler
    {
        int protocolversion;
        Dictionary<PacketIncomingType, IPacketHandler> packetHandlers = new Dictionary<PacketIncomingType, IPacketHandler>();
        public Protocol18PacketHandler(int protocolVersion, DataTypes dataTypes, IMinecraftComHandler handler, IPacketSender packetSender, Protocol18Terrain pTerrain, Protocol18Forge pForge, WorldInfo worldInfo, Protocol18Handler protocol18Handler)
        {
            this.protocolversion = protocolVersion;

            MultiVersionHandler blockHandler = new MultiVersionHandler(protocolVersion);
            blockHandler.addPacketHandler(new BlockChangeHandler17(handler, dataTypes), (int)McVersion.V17).
                addPacketHandler(new BlockChangeHandler18(handler, dataTypes), (int)McVersion.V18);
            packetHandlers.Add(PacketIncomingType.BlockChange, blockHandler);


            packetHandlers.Add(PacketIncomingType.KeepAlive, new KeepAliveHandler(packetSender));
            packetHandlers.Add(PacketIncomingType.JoinGame, new JoinGameHandler(handler, dataTypes, worldInfo, protocolVersion));
            packetHandlers.Add(PacketIncomingType.ChatMessage, new ChatMessageHandler(handler, dataTypes));
            packetHandlers.Add(PacketIncomingType.Respawn, new RespawnHandler(handler, dataTypes, worldInfo, protocolVersion));
            packetHandlers.Add(PacketIncomingType.PlayerPositionAndLook, new PlayerPositionAndLookHandler(packetSender, dataTypes, handler, protocolVersion));
            packetHandlers.Add(PacketIncomingType.ChunkData, new ChunkDataHandler(handler, dataTypes, pTerrain, worldInfo, protocolVersion));
            packetHandlers.Add(PacketIncomingType.MultiBlockChange, new MultiBlockChangeHandler(handler, dataTypes, protocolVersion));

            packetHandlers.Add(PacketIncomingType.MapChunkBulk, new MapChunkBulkHandler(handler, dataTypes, protocolVersion, pTerrain, worldInfo));
            packetHandlers.Add(PacketIncomingType.UnloadChunk, new UnloadChunkHandler(handler, dataTypes, protocolVersion));
            packetHandlers.Add(PacketIncomingType.PlayerListUpdate, new PlayerListUpdateHandler(handler, dataTypes, protocolVersion));
            packetHandlers.Add(PacketIncomingType.TabCompleteResult, new TabCompleteResultHandler(handler, dataTypes, protocol18Handler, protocolVersion));
            packetHandlers.Add(PacketIncomingType.PluginMessage, new PluginMessageHandler(handler, dataTypes, pForge, worldInfo, protocolVersion));
            packetHandlers.Add(PacketIncomingType.KickPacket, new KickHandler(handler, dataTypes));
            packetHandlers.Add(PacketIncomingType.NetworkCompressionTreshold, new NetworkCompressionThresholdHandler(handler, dataTypes, protocol18Handler, protocolVersion));
            packetHandlers.Add(PacketIncomingType.OpenWindow, new OpenWindowHandler(handler, dataTypes));
            packetHandlers.Add(PacketIncomingType.CloseWindow, new CloseWindowHandler(handler, dataTypes));
            packetHandlers.Add(PacketIncomingType.WindowItems, new WindowItemsHandler(handler, dataTypes));
            packetHandlers.Add(PacketIncomingType.ResourcePackSend, new ResourecePackSendHandler(dataTypes, packetSender, protocolVersion));
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> packetData)
        {
            IPacketHandler packetHandler;
            if (packetHandlers.TryGetValue(packetType, out packetHandler))
            {
                packetHandler.HandlePacket(packetType, packetData);
                return true;
            }

            return false;
        }
    }
}
