using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.Protocol18.Handlers;
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

        internal const int MC18Version = 47;
        internal const int MC19Version = 107;
        internal const int MC191Version = 108;
        internal const int MC110Version = 210;
        internal const int MC1112Version = 316;
        internal const int MC112Version = 335;
        internal const int MC1121Version = 338;
        internal const int MC1122Version = 340;
        internal const int MC113Version = 393;
        internal const int MC114Version = 477;
        internal const int MC1142Version = 485;


        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private int autocomplete_transaction_id = 0;
        private readonly List<string> autocomplete_result = new List<string>();
        private bool login_phase = true;
        private int currentDimension;


        int protocolversion;
        Protocol18Forge pForge;
        Protocol18Terrain pTerrain;
        IMinecraftComHandler handler;
        SocketWrapper socketWrapper;
        DataTypes dataTypes;
        Thread netRead;



        Dictionary<PacketIncomingType, IPacketHandler> packetHandlers;


        public Protocol18PacketHandler(int protocolVersion, DataTypes dataTypes, IMinecraftComHandler handler)
        {
            this.protocolversion = protocolVersion;
            this.dataTypes = dataTypes;
            this.handler = handler;

            packetHandlers.Add(PacketIncomingType.KeepAlive, new KeepAliveHandler());
            packetHandlers.Add(PacketIncomingType.JoinGame, new JoinGameHandler());
            packetHandlers.Add(PacketIncomingType.ChatMessage, new ChatMessageHandler());
            packetHandlers.Add(PacketIncomingType.Respawn, new RespawnHandler());
            packetHandlers.Add(PacketIncomingType.PlayerPositionAndLook, new PlayerPositionAndLookHandler());
            packetHandlers.Add(PacketIncomingType.ChunkData, new ChunkDataHandler());
            packetHandlers.Add(PacketIncomingType.MultiBlockChange, new MultiBlockChangeHandler());
            packetHandlers.Add(PacketIncomingType.BlockChange, new BlockChangeHandler());
            packetHandlers.Add(PacketIncomingType.MapChunkBulk, new MapChunkBulkHandler());
            packetHandlers.Add(PacketIncomingType.UnloadChunk, new UnloadChunkHandler());
            packetHandlers.Add(PacketIncomingType.PlayerListUpdate, new PlayerListUpdateHandler());
            packetHandlers.Add(PacketIncomingType.TabCompleteResult, new TabCompleteResultHandler());
            packetHandlers.Add(PacketIncomingType.PluginMessage, new PluginMessageHandler());
            packetHandlers.Add(PacketIncomingType.KickPacket, new KickHandler());
            packetHandlers.Add(PacketIncomingType.NetworkCompressionTreshold, new NetworkCompressionThresholdHandler());
            packetHandlers.Add(PacketIncomingType.OpenWindow, new OpenWindowHandler());
            packetHandlers.Add(PacketIncomingType.CloseWindow, new CloseWindowHandler());
            packetHandlers.Add(PacketIncomingType.WindowItems, new WindowItemsHandler());
            packetHandlers.Add(PacketIncomingType.ResourcePackSend, new ResourecePackSendHandler());
        }
        public bool handlePacket(PacketIncomingType packetType, List<byte> packetData)
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
