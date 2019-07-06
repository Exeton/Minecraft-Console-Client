using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Mapping;
using MinecraftClient.Data;
using MinecraftClient.Net;
using MinecraftClient.View;
using MinecraftClient.MVVM.Client;
using MinecraftClient.Protocol.Session;

namespace MinecraftClient
{
    public class McTcpClient : IMinecraftComHandler
    {
        public static int ReconnectionAttemptsLeft = 0;

        private readonly Dictionary<Guid, string> onlinePlayers = new Dictionary<Guid, string>();

        public Player player;
        private World world = new World();

        private string host;
        private int port;
        private string sessionid;

        PacketClient packetClient;

        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }

        public string GetSessionID() { return sessionid; }
        public World GetWorld() { return world; }

        public IMinecraftCom handler;


        public McTcpClient(SessionToken session)
        {
            //Get server version
            int protocolversion = 0;
            ForgeInfo forgeInfo = null;

            if (Settings.ServerConnectionInfo.ServerVersion != "" && Settings.ServerConnectionInfo.ServerVersion.ToLower() != "auto")
            {
                protocolversion = Protocol.ProtocolHandler.MCVer2ProtocolVersion(Settings.ServerConnectionInfo.ServerVersion);

                if (protocolversion != 0)
                {
                    ConsoleIO.WriteLineFormatted("§8Using Minecraft version " + Settings.ServerConnectionInfo.ServerVersion + " (protocol v" + protocolversion + ')');
                }
                else ConsoleIO.WriteLineFormatted("§8Unknown or not supported MC version '" + Settings.ServerConnectionInfo.ServerVersion + "'.\nSwitching to autodetection mode.");
            }

            if (protocolversion == 0)
            {
                Console.WriteLine("Retrieving Server Info...");
                if (!ProtocolHandler.GetServerInfo(Settings.ServerConnectionInfo.ServerIP, Settings.ServerConnectionInfo.ServerPort, ref protocolversion, ref forgeInfo))
                {
                    //HandleFailure("Failed to ping this IP.", true, DisconnectReason.ConnectionLost);
                    return;
                }
            }

            if (protocolversion != 0)
            {
                try
                {
                    StartClient(session.PlayerName, session.PlayerID, session.ID, protocolversion, Settings.ServerConnectionInfo.ServerIP, Settings.ServerConnectionInfo.ServerPort, forgeInfo);

                    //Update console title
                    if (Settings.ConsoleTitle != "")
                        Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);
                }
                catch (NotSupportedException) {/* HandleFailure("Cannot connect to the server : This version is not supported !", true); */ }
            }
            //else HandleFailure("Failed to determine server version.", true);
        }

        private void StartClient(string user, string uuid, string sessionID, int protocolversion, string server_ip, ushort port, ForgeInfo forgeInfo)
        {
            this.sessionid = sessionID;
            this.host = server_ip;
            this.port = port;
            player = new Player(world, user, uuid);

            try
            {
                packetClient = new PacketClient();
                if (!packetClient.Connect(new ServerConnectionInfo(server_ip, port), player, protocolversion, forgeInfo, this))
                    return;

                handler = packetClient.communicationHandler;
                player.SetHandler(handler);

                Console.WriteLine("Server was successfully joined.\nType '" + Settings.internalCmdChar + "quit' to leave the server.");
            }
            catch (SocketException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.Message);
                Console.WriteLine("Failed to connect to this IP.");
            }
        }

        public void OnGameJoined()
        {
            if (!String.IsNullOrWhiteSpace(Settings.BrandInfo))
                handler.SendBrandInfo(Settings.BrandInfo.Trim());
            if (Settings.MCSettings_Enabled)
                handler.SendClientSettings(
                    Settings.MCSettings_Locale,
                    Settings.MCSettings_RenderDistance,
                    Settings.MCSettings_Difficulty,
                    Settings.MCSettings_ChatMode,
                    Settings.MCSettings_ChatColors,
                    Settings.MCSettings_Skin_All,
                    Settings.MCSettings_MainHand);
            player.OnJoin();
        }

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        public void OnRespawn()
        {
            //Won't work with connection pool
            world.Clear();
            
        }
        public void OnTextReceived(string text, bool isJson)
        {
            List<string> links = new List<string>();
            string json = null;
            if (isJson)
            {
                json = text;
                text = ChatParser.ParseText(json, links);
            }
            ConsoleIO.WriteLineFormatted(text, true);
            if (Settings.DisplayChatLinks)
                foreach (string link in links)
                    ConsoleIO.WriteLineFormatted("§8MCC: Link: " + link, false);
        }
        public void OnConnectionLost(DisconnectReason reason, string message)
        {
            world.Clear();

            bool will_restart = false;

            switch (reason)
            {
                case DisconnectReason.ConnectionLost:
                    message = "Connection has been lost.";
                    ConsoleIO.WriteLine(message);
                    break;

                case DisconnectReason.InGameKick:
                    ConsoleIO.WriteLine("Disconnected by Server :");
                    ConsoleIO.WriteLineFormatted(message);
                    break;

                case DisconnectReason.LoginRejected:
                    ConsoleIO.WriteLine("Login failed :");
                    ConsoleIO.WriteLineFormatted(message);
                    break;
            }
        }

        //Ticks 20 times per second
        public void OnUpdate()
        {
            packetClient.Update();
            player.OnUpdate();
        }

        public bool SendText(string text)
        {
            int maxLength = handler.GetMaxChatMessageLength();
            if (text.Length > maxLength) //Message is too long?
            {
                if (text[0] == '/')
                {
                    //Send the first 100/256 chars of the command
                    text = text.Substring(0, maxLength);
                    return handler.SendChatMessage(text);
                }
                else
                {
                    //Send the message splitted into several messages
                    while (text.Length > maxLength)
                    {
                        handler.SendChatMessage(text.Substring(0, maxLength));
                        text = text.Substring(maxLength, text.Length - maxLength);
                        if (Settings.splitMessageDelay.TotalSeconds > 0)
                            Thread.Sleep(Settings.splitMessageDelay);
                    }
                    return handler.SendChatMessage(text);
                }
            }
            else return handler.SendChatMessage(text);
        }

        public bool SendRespawnPacket()
        {
            return handler.SendRespawnPacket();
        }
        public void OnPlayerJoin(Guid uuid, string name)
        {
            //Ignore placeholders eg 0000tab# from TabListPlus

            lock (onlinePlayers)
            {
                onlinePlayers[uuid] = name;
            }
        }
        public void OnPlayerLeave(Guid uuid)
        {
            lock (onlinePlayers)
            {
                onlinePlayers.Remove(uuid);
            }
        }
        public string[] GetOnlinePlayers()
        {
            lock (onlinePlayers)
            {
                return onlinePlayers.Values.Distinct().ToArray();
            }
        }

        /// <summary>
        /// Get a dictionary of online player names and their corresponding UUID
        /// </summary>
        /// <returns>
        ///     dictionary of online player whereby
        ///     UUID represents the key
        ///     playername represents the value</returns>
        public Dictionary<string, string> GetOnlinePlayersWithUUID()
        {
            Dictionary<string, string> uuid2Player = new Dictionary<string, string>();
            lock (onlinePlayers)
            {
                foreach (Guid key in onlinePlayers.Keys)
                {
                    uuid2Player.Add(key.ToString(), onlinePlayers[key]);
                }
            }
            return uuid2Player;
        }

        public bool SendPluginChannelMessage(string channel, byte[] data, bool sendEvenIfNotRegistered = false)
        {
            return packetClient.SendPluginChannelMessage(channel, data, sendEvenIfNotRegistered);
        }

        public void OnPluginChannelMessage(string channel, byte[] data)
        {
            packetClient.OnPluginChannelMessage(channel, data);
        }
    }
}
