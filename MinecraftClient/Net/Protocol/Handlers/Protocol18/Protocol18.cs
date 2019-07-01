using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Proxy;
using System.Security.Cryptography;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Handlers.Protocol18;
using MinecraftClient.Data;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.7.X+ Protocols
    /// </summary>
    /// <remarks>
    /// Typical update steps for implementing protocol changes for a new Minecraft version:
    ///  - Perform a diff between latest supported version in MCC and new stable version to support on https://wiki.vg/Protocol
    ///  - If there are any changes in packets implemented by MCC, add MCXXXVersion field below and implement new packet layouts
    ///  - If packet IDs were changed, also update getPacketIncomingType() and getPacketOutgoingID() inside Protocol18PacketTypes.cs
    /// </remarks>
    class Protocol18Handler : IMinecraftCom
    {

        public bool autocomplete_received = false;
        public int autocomplete_transaction_id = 0;
        public readonly List<string> autocomplete_result = new List<string>();
        private bool login_phase = true;
        private int protocolversion;
        private WorldInfo worldInfo = new WorldInfo();

        Protocol18Forge pForge;
        Protocol18Terrain pTerrain;
        IMinecraftComHandler handler;
        public ConnectionInfo connectionInfo;
        DataTypes dataTypes;
        Thread netRead;
        Player player;

        IPacketReadWriter packetReadWriter;
        IPacketHandler packetHandler;

        public Protocol18Handler(TcpClient Client, int protocolVersion, IMinecraftComHandler handler, ForgeInfo forgeInfo, Player player)
        {

            this.player = player;

            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();

            connectionInfo = new ConnectionInfo(new SocketWrapper(Client), 0);
            this.dataTypes = new DataTypes(protocolVersion);
            this.protocolversion = protocolVersion;
            this.handler = handler;
            this.pForge = new Protocol18Forge(forgeInfo, protocolVersion, dataTypes, this, handler);
            this.pTerrain = new Protocol18Terrain(protocolVersion, dataTypes, handler);

            if (handler.GetTerrainEnabled() && protocolversion > (int)McVersion.V1142)
            {
                ConsoleIO.WriteLineFormatted("§8Terrain & Movements currently not handled for that MC version.");
                handler.SetTerrainEnabled(false);
            }

            if (player.GetInventoryEnabled() && protocolversion > (int)McVersion.V114)
            {
                ConsoleIO.WriteLineFormatted("§8Inventories are currently not handled for that MC version.");
                player.SetInventoryEnabled(false);
            }

            if (protocolversion >= (int)McVersion.V18)
            {
                if (protocolVersion > (int)McVersion.V1142 && handler.GetTerrainEnabled())
                    throw new NotImplementedException("Please update block types handling for this Minecraft version. See Material.cs");
                if (protocolVersion >= (int)McVersion.V114)
                    Block.Palette = new Palette114();
                else Block.Palette = new Palette113();
            }
            else Block.Palette = new Palette112();

            packetReadWriter = new Protocol18PacketReadWriter(connectionInfo, dataTypes, protocolVersion);
            packetHandler = new Protocol18PacketHandler(protocolVersion, dataTypes, handler, packetReadWriter, pTerrain, pForge, worldInfo, this, player);

            pForge.packetReadWriter = packetReadWriter;
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        private void Updater()
        {
            try
            {
                do
                {
                    Thread.Sleep(100);
                }
                while (Update());
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        private bool Update()
        {
            handler.OnUpdate();
            if (!connectionInfo.socketWrapper.IsConnected())
                return false;
            try
            {
                while (connectionInfo.socketWrapper.HasDataAvailable())
                {
                    Packet packet = packetReadWriter.ReadNext();
                    HandlePacket(packet);
                }
            }
            catch (SocketException) { return false; }
            catch (NullReferenceException) { return false; }
            return true;
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        internal bool HandlePacket(Packet packet)
        {
            int packetID = packet.id;
            List<byte> packetData = packet.data;

            try
            {
                if (login_phase)
                {
                    switch (packetID) //Packet IDs are different while logging in
                    {
                        case 0x03:
                            if (protocolversion >= (int)McVersion.V18)
                                connectionInfo.compressionThreshold = dataTypes.ReadNextVarInt(packetData);
                            break;
                        default:
                            return false; //Ignored packet
                    }
                }
                return packetHandler.HandlePacket(Protocol18PacketTypes.GetPacketIncomingType(packetID, protocolversion), packet.data);
            }
            catch (Exception innerException)
            {
                throw new System.IO.InvalidDataException(
                    String.Format("Failed to process incoming packet of type {0}. (PacketID: {1}, Protocol: {2}, LoginPhase: {3}, InnerException: {4}).",
                        Protocol18PacketTypes.GetPacketIncomingType(packetID, protocolversion),
                        packetID,
                        protocolversion,
                        login_phase,
                        innerException.GetType()),
                    innerException);
            }
        }

        /// <summary>
        /// Start the updating thread. Should be called after login success.
        /// </summary>
        private void StartUpdating()
        {
            netRead = new Thread(new ThreadStart(Updater));
            netRead.Name = "ProtocolPacketHandler";
            netRead.Start();
        }

        /// <summary>
        /// Disconnect from the server, cancel network reading.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (netRead != null)
                {
                    netRead.Abort();
                    connectionInfo.socketWrapper.Disconnect();
                }
            }
            catch { }
        }


        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login()
        {
            byte[] protocol_version = dataTypes.GetVarInt(protocolversion);
            string server_address = pForge.GetServerAddress(handler.GetServerHost());
            byte[] server_port = BitConverter.GetBytes((ushort)handler.GetServerPort()); Array.Reverse(server_port);
            byte[] next_state = dataTypes.GetVarInt(2);
            byte[] handshake_packet = dataTypes.ConcatBytes(protocol_version, dataTypes.GetString(server_address), server_port, next_state);

            packetReadWriter.WritePacket(0x00, handshake_packet);

            byte[] login_packet = dataTypes.GetString(player.GetUsername());

            packetReadWriter.WritePacket(0x00, login_packet);

            while (true)
            {
                Packet packet = packetReadWriter.ReadNext();
                if (packet.id == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packet.data)));
                    return false;
                }
                else if (packet.id == 0x01) //Encryption request
                {
                    string serverID = dataTypes.ReadNextString(packet.data);
                    byte[] Serverkey = dataTypes.ReadNextByteArray(packet.data);
                    byte[] token = dataTypes.ReadNextByteArray(packet.data);
                    return StartEncryption(player.GetUserUUID(), handler.GetSessionID(), token, serverID, Serverkey);
                }
                else if (packet.id == 0x02) //Login successful
                {
                    ConsoleIO.WriteLineFormatted("§8Server is in offline mode.");
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                        return false;

                    StartUpdating();
                    return true; //No need to check session or start encryption
                }
                else HandlePacket(packet);
            }
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>
        private bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, byte[] serverKey)
        {
            RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverKey);
            byte[] secretKey = CryptoHandler.GenerateAESPrivateKey();

            if (Settings.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8Crypto keys & hash generated.");

            if (serverIDhash != "-")
            {
                Console.WriteLine("Checking Session...");
                if (!ProtocolHandler.SessionCheck(uuid, sessionID, CryptoHandler.getServerHash(serverIDhash, serverKey, secretKey)))
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, "Failed to check session.");
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = dataTypes.GetArray(RSAService.Encrypt(secretKey, false));
            byte[] token_enc = dataTypes.GetArray(RSAService.Encrypt(token, false));

            //Encryption Response packet
            packetReadWriter.WritePacket(0x01, dataTypes.ConcatBytes(key_enc, token_enc));

            //Start client-side encryption
            connectionInfo.socketWrapper.SwitchToEncrypted(secretKey);

            //Process the next packet
            while (true)
            {
                Packet packet = packetReadWriter.ReadNext();
                if (packet.id == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packet.data)));
                    return false;
                }
                else if (packet.id == 0x02) //Login successful
                {
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                        return false;

                    StartUpdating();
                    return true;
                }
                else HandlePacket(packet);
            }
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return protocolversion > (int)McVersion.V110
                ? 256
                : 100;
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatMessage(string message)
        {
            if (String.IsNullOrEmpty(message))
                return true;
            try
            {
                byte[] message_packet = dataTypes.GetString(message);
                packetReadWriter.WritePacket(PacketOutgoingType.ChatMessage, message_packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <returns>True if properly sent</returns>
        public bool SendRespawnPacket()
        {
            try
            {
                packetReadWriter.WritePacket(PacketOutgoingType.ClientStatus, new byte[] { 0 });
                return true;
            }
            catch (SocketException) { return false; }
        }

        /// <summary>
        /// Tell the server what client is being used to connect to the server
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>
        public bool SendBrandInfo(string brandInfo)
        {
            if (String.IsNullOrEmpty(brandInfo))
                return false;
            // Plugin channels were significantly changed between Minecraft 1.12 and 1.13
            // https://wiki.vg/index.php?title=Pre-release_protocol&oldid=14132#Plugin_Channels
            if (protocolversion >= (int)McVersion.V113)
            {
                return SendPluginChannelPacket("minecraft:brand", dataTypes.GetString(brandInfo));
            }
            else
            {
                return SendPluginChannelPacket("MC|Brand", dataTypes.GetString(brandInfo));
            }
        }

        /// <summary>
        /// Inform the server of the client's Minecraft settings
        /// </summary>
        /// <param name="language">Client language eg en_US</param>
        /// <param name="viewDistance">View distance, in chunks</param>
        /// <param name="difficulty">Game difficulty (client-side...)</param>
        /// <param name="chatMode">Chat mode (allows muting yourself)</param>
        /// <param name="chatColors">Show chat colors</param>
        /// <param name="skinParts">Show skin layers</param>
        /// <param name="mainHand">1.9+ main hand</param>
        /// <returns>True if client settings were successfully sent</returns>
        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetString(language));
                fields.Add(viewDistance);
                fields.AddRange(protocolversion >= (int)McVersion.V19
                    ? dataTypes.GetVarInt(chatMode)
                    : new byte[] { chatMode });
                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolversion < (int)McVersion.V18)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);
                if (protocolversion >= (int)McVersion.V19)
                    fields.AddRange(dataTypes.GetVarInt(mainHand));
                packetReadWriter.WritePacket(PacketOutgoingType.ClientSettings, fields);
            }
            catch (SocketException) { }
            return false;
        }

        /// <summary>
        /// Send a location update to the server
        /// </summary>
        /// <param name="location">The new location of the player</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <param name="yaw">Optional new yaw for updating player look</param>
        /// <param name="pitch">Optional new pitch for updating player look</param>
        /// <returns>True if the location update was successfully sent</returns>
        public bool SendLocationUpdate(Location location, bool onGround, float? yaw = null, float? pitch = null)
        {
            if (handler.GetTerrainEnabled())
            {
                byte[] yawpitch = new byte[0];
                PacketOutgoingType packetType = PacketOutgoingType.PlayerPosition;

                if (yaw.HasValue && pitch.HasValue)
                {
                    yawpitch = dataTypes.ConcatBytes(dataTypes.GetFloat(yaw.Value), dataTypes.GetFloat(pitch.Value));
                    packetType = PacketOutgoingType.PlayerPositionAndLook;
                }

                try
                {
                    packetReadWriter.WritePacket(packetType, dataTypes.ConcatBytes(
                        dataTypes.GetDouble(location.X),
                        dataTypes.GetDouble(location.Y),
                        protocolversion < (int)McVersion.V18
                            ? dataTypes.GetDouble(location.Y + 1.62)
                            : new byte[0],
                        dataTypes.GetDouble(location.Z),
                        yawpitch,
                        new byte[] { onGround ? (byte)1 : (byte)0 }));
                    return true;
                }
                catch (SocketException) { return false; }
            }
            else return false;
        }

        /// <summary>
        /// Send a plugin channel packet (0x17) to the server, compression and encryption will be handled automatically
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            try
            {
                // In 1.7, length needs to be included.
                // In 1.8, it must not be.
                if (protocolversion < (int)McVersion.V18)
                {
                    byte[] length = BitConverter.GetBytes((short)data.Length);
                    Array.Reverse(length);

                    packetReadWriter.WritePacket(PacketOutgoingType.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), length, data));
                }
                else
                {
                    packetReadWriter.WritePacket(PacketOutgoingType.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), data));
                }

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            connectionInfo.socketWrapper.Disconnect();
        }

        /// <summary>
        /// Autocomplete text while typing username or command
        /// </summary>
        /// <param name="BehindCursor">Text behind cursor</param>
        /// <returns>Completed text</returns>
        IEnumerable<string> IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return new string[] { };

            byte[] transaction_id = dataTypes.GetVarInt(autocomplete_transaction_id);
            byte[] assume_command = new byte[] { 0x00 };
            byte[] has_position = new byte[] { 0x00 };

            byte[] tabcomplete_packet = new byte[] { };

            if (protocolversion >= (int)McVersion.V18)
            {
                if (protocolversion >= (int)McVersion.V113)
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, transaction_id);
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));
                }
                else
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));

                    if (protocolversion >= (int)McVersion.V19)
                    {
                        tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, assume_command);
                    }

                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, has_position);
                }
            }
            else
            {
                tabcomplete_packet = dataTypes.ConcatBytes(dataTypes.GetString(BehindCursor));
            }

            autocomplete_received = false;
            autocomplete_result.Clear();
            autocomplete_result.Add(BehindCursor);
            packetReadWriter.WritePacket(PacketOutgoingType.TabComplete, tabcomplete_packet);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
            if (autocomplete_result.Count > 0)
                ConsoleIO.WriteLineFormatted("§8" + String.Join(" ", autocomplete_result), false);
            return autocomplete_result;
        }

        /// <summary>
        /// Ping a Minecraft server to get information about the server
        /// </summary>
        /// <returns>True if ping was successful</returns>
        public static bool doPing(string host, int port, ref int protocolversion, ref ForgeInfo forgeInfo)
        {
            string version = "";
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveBufferSize = 1024 * 1024;
            SocketWrapper socketWrapper = new SocketWrapper(tcp);
            DataTypes dataTypes = new DataTypes((int)McVersion.V18);

            byte[] packet_id = dataTypes.GetVarInt(0);
            byte[] protocol_version = dataTypes.GetVarInt(-1);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = dataTypes.GetVarInt(1);
            byte[] packet = dataTypes.ConcatBytes(packet_id, protocol_version, dataTypes.GetString(host), server_port, next_state);
            byte[] tosend = dataTypes.ConcatBytes(dataTypes.GetVarInt(packet.Length), packet);

            socketWrapper.SendDataRAW(tosend);

            byte[] status_request = dataTypes.GetVarInt(0);
            byte[] request_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(status_request.Length), status_request);

            socketWrapper.SendDataRAW(request_packet);

            int packetLength = dataTypes.ReadNextVarIntRAW(socketWrapper);
            if (packetLength > 0) //Read Response length
            {
                List<byte> packetData = new List<byte>(socketWrapper.ReadDataRAW(packetLength));
                if (dataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = dataTypes.ReadNextString(packetData); //Get the Json data

                    if (!String.IsNullOrEmpty(result) && result.StartsWith("{") && result.EndsWith("}"))
                    {
                        Json.JSONData jsonData = Json.ParseJson(result);
                        if (jsonData.Type == Json.JSONData.DataType.Object && jsonData.Properties.ContainsKey("version"))
                        {
                            Json.JSONData versionData = jsonData.Properties["version"];

                            //Retrieve display name of the Minecraft version
                            if (versionData.Properties.ContainsKey("name"))
                                version = versionData.Properties["name"].StringValue;

                            //Retrieve protocol version number for handling this server
                            if (versionData.Properties.ContainsKey("protocol"))
                                protocolversion = dataTypes.Atoi(versionData.Properties["protocol"].StringValue);

                            // Check for forge on the server.
                            Protocol18Forge.ServerInfoCheckForge(jsonData, ref forgeInfo);

                            ConsoleIO.WriteLineFormatted("§8Server version : " + version + " (protocol v" + protocolversion + (forgeInfo != null ? ", with Forge)." : ")."));

                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}