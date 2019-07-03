using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using MinecraftClient.Protocol;
using MinecraftClient.Proxy;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Mapping;
using MinecraftClient.Commands;
using MinecraftClient.Data;
using MinecraftClient.API;
using MinecraftClient.Net;
using MinecraftClient.View;

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

        List<IPlugin> plugins;
        PacketClient packetClient;


        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }

        public string GetSessionID() { return sessionid; }
        public World GetWorld() { return world; }

        public IMinecraftCom handler;
        Thread cmdprompt;

        public McTcpClient(string username, string uuid, string sessionID, int protocolversion, string server_ip, ushort port, ForgeInfo forgeInfo, List<IPlugin> plugins)
        {
            StartClient(username, uuid, sessionID, protocolversion, server_ip, port, forgeInfo, plugins);
        }

        private void StartClient(string user, string uuid, string sessionID, int protocolversion, string server_ip, ushort port, ForgeInfo forgeInfo, List<IPlugin> plugins)
        {
            this.sessionid = sessionID;
            this.host = server_ip;
            this.port = port;
            this.plugins = plugins;
            player = new Player(world, user, uuid);

            try
            {

                packetClient = new PacketClient();
                bool connected = packetClient.Connect(new ServerConnectionInfo("127.0.0.1", 25565), player, protocolversion, forgeInfo, this);

                if (!connected)
                    return;

                this.handler = packetClient.communicationHandler;

                player.SetHandler(handler);

                foreach (IPlugin plugin in plugins)
                    plugin.OnJoin();

                Console.WriteLine("Server was successfully joined.\nType '"
                    + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar)
                    + "quit' to leave the server.");

                cmdprompt = new Thread(new ThreadStart(ConsoleInputHandler));
                cmdprompt.Name = "MCC Command prompt";
                cmdprompt.Start();

            }
            catch (SocketException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.Message);
                Console.WriteLine("Failed to connect to this IP.");
                Client.Client.HandleFailure();
            }
        }
        private void ConsoleInputHandler()
        {
            try
            {
                while (packetClient.client.Client.Connected)
                {
                    string text = ConsoleIO.ReadLine();
                    if (text.Length > 0 && text[0] == (char)0x00)
                    {
                        //Process a request from the GUI
                        string[] command = text.Substring(1).Split((char)0x00);
                        switch (command[0].ToLower())
                        {
                            case "autocomplete":
                                if (command.Length > 1) { ConsoleIO.WriteLine((char)0x00 + "autocomplete" + (char)0x00 + handler.AutoComplete(command[1])); }
                                else Console.WriteLine((char)0x00 + "autocomplete" + (char)0x00);
                                break;
                        }
                    }
                    else
                    {
                        text = text.Trim();
                        if (text.Length > 0)
                        {
                            if (Settings.internalCmdChar == ' ' || text[0] == Settings.internalCmdChar)
                            {
                                string command = Settings.internalCmdChar == ' ' ? text : text.Substring(1);
                                CommandHandler commandHandler = Program.CommandHandler;

                                if (!commandHandler.runCommand(command) && Settings.internalCmdChar == '/')
                                {
                                    SendText(text);
                                }
                            }
                            else SendText(text);
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
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
            //Won't work with multiplayer
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

                foreach (IPlugin plugin in plugins)
                {
                    plugin.OnText(text);
                }
            }
            ConsoleIO.WriteLineFormatted(text, true);
            if (Settings.DisplayChatLinks)
                foreach (string link in links)
                    ConsoleIO.WriteLineFormatted("§8MCC: Link: " + link, false);
        }

        public void OnConnectionLost(ChatBot.DisconnectReason reason, string message)
        {
            world.Clear();

            bool will_restart = false;

            switch (reason)
            {
                case ChatBot.DisconnectReason.ConnectionLost:
                    message = "Connection has been lost.";
                    ConsoleIO.WriteLine(message);
                    break;

                case ChatBot.DisconnectReason.InGameKick:
                    ConsoleIO.WriteLine("Disconnected by Server :");
                    ConsoleIO.WriteLineFormatted(message);
                    break;

                case ChatBot.DisconnectReason.LoginRejected:
                    ConsoleIO.WriteLine("Login failed :");
                    ConsoleIO.WriteLineFormatted(message);
                    break;
            }

            if (!will_restart)
                Client.Client.HandleFailure();
        }

        /// <summary>
        /// Called ~10 times per second by the protocol handler
        /// </summary>
        public void OnUpdate()
        {
            foreach (IPlugin plugin in plugins)
            {
                try
                {
                    plugin.OnUpdate();
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted("§8Update: Got error from " + plugin.ToString() + ": " + e.ToString());
                    if (e is ThreadAbortException)
                        throw;
                }
            }

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
            if (!ChatBot.IsValidName(name))
                return;

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

        public void RegisterPluginChannel(string channel, ChatBot bot)
        {
            packetClient.RegisterPluginChannel(channel, bot);
        }

        public void UnregisterPluginChannel(string channel, ChatBot bot)
        {
            packetClient.UnregisterPluginChannel(channel, bot);
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
