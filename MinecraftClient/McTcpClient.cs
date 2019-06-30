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

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// </summary>
    public class McTcpClient : IMinecraftComHandler
    {
        public static int ReconnectionAttemptsLeft = 0;

        private readonly Dictionary<Guid, string> onlinePlayers = new Dictionary<Guid, string>();

        private List<ChatBot> bots = new List<ChatBot>();

        private readonly Dictionary<string, List<ChatBot>> registeredBotPluginChannels = new Dictionary<string, List<ChatBot>>();
        private readonly List<string> registeredServerPluginChannels = new List<String>();

        public static bool terrainAndMovementsEnabled;
        public static bool terrainAndMovementsRequested = false;

        public Player player;
        private World world = new World();

        private string host;
        private int port;
        private string sessionid;


        List<IPlugin> plugins;

        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }

        public string GetSessionID() { return sessionid; }
        public World GetWorld() { return world; }

        TcpClient client;
        IMinecraftCom handler;
        Thread cmdprompt;

        /// <summary>
        /// Starts the main chat client
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="uuid">The player's UUID for online-mode authentication</param>
        /// <param name="sessionID">A valid sessionID obtained after logging in</param>
        /// <param name="server_ip">The server IP</param>
        /// <param name="port">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>
        public McTcpClient(string username, string uuid, string sessionID, int protocolversion, ForgeInfo forgeInfo, string server_ip, ushort port, List<IPlugin> plugins)
        {
            StartClient(username, uuid, sessionID, server_ip, port, protocolversion, forgeInfo, plugins);
        }

        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="user">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="server_ip">The server IP</param>
        /// <param name="port">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>
        /// <param name="uuid">The player's UUID for online-mode authentication</param>
        private void StartClient(string user, string uuid, string sessionID, string server_ip, ushort port, int protocolversion, ForgeInfo forgeInfo, List<IPlugin> plugins)
        {
            terrainAndMovementsEnabled = Settings.TerrainAndMovements;

            bool retry = false;
            this.sessionid = sessionID;
            this.host = server_ip;
            this.port = port;
            this.plugins = plugins;
            player = new Player(world, user, uuid);

            if (bots.Count == 0)
            {


                if (true) { BotLoad(new ChatBots.AntiAFK(60)); }
                //if (Settings.AntiAFK_Enabled) { BotLoad(new ChatBots.AntiAFK(Settings.AntiAFK_Delay)); }
                if (Settings.Hangman_Enabled) { BotLoad(new ChatBots.HangmanGame(Settings.Hangman_English)); }
                if (Settings.Alerts_Enabled) { BotLoad(new ChatBots.Alerts()); }
                if (Settings.ChatLog_Enabled) { BotLoad(new ChatBots.ChatLog(Settings.ExpandVars(Settings.ChatLog_File), Settings.ChatLog_Filter, Settings.ChatLog_DateTime)); }
                if (Settings.PlayerLog_Enabled) { BotLoad(new ChatBots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.ExpandVars(Settings.PlayerLog_File))); }
                if (Settings.AutoRelog_Enabled) { BotLoad(new ChatBots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries)); }
                if (Settings.ScriptScheduler_Enabled) { BotLoad(new ChatBots.ScriptScheduler(Settings.ExpandVars(Settings.ScriptScheduler_TasksFile))); }
                if (Settings.RemoteCtrl_Enabled) { BotLoad(new ChatBots.RemoteControl()); }
                if (Settings.AutoRespond_Enabled) { BotLoad(new ChatBots.AutoRespond(Settings.AutoRespond_Matches)); }
                //Add your ChatBot here by uncommenting and adapting
                //BotLoad(new ChatBots.YourBot());
            }
            
            try
            {
                client = ProxyHandler.newTcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;

                handler = ProtocolHandler.GetProtocolHandler(client, protocolversion, forgeInfo, this, player);
                player.SetHandler(handler);


                Console.WriteLine("Version is supported.\nLogging in...");

                try
                {
                    if (handler.Login())
                    {
                        foreach (ChatBot bot in bots)
                            bot.AfterGameJoined();

                        foreach (IPlugin plugin in plugins)
                            plugin.OnJoin();
                        //BotLoad(bot, false);

                        Console.WriteLine("Server was successfully joined.\nType '"
                            + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar)
                            + "quit' to leave the server.");

                        cmdprompt = new Thread(new ThreadStart(CommandPrompt));
                        cmdprompt.Name = "MCC Command prompt";
                        cmdprompt.Start();

                        Thread respawnPacket = new Thread(new ThreadStart(() =>{
                            Thread.Sleep(500);
                            handler.SendRespawnPacket();
                        }));
                        respawnPacket.Start();                    
                    }
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.Message);
                    Console.WriteLine("Failed to join this server.");
                    retry = true;
                }
            }
            catch (SocketException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.Message);
                Console.WriteLine("Failed to connect to this IP.");
                retry = true;
            }

            if (retry)
            {
                if (ReconnectionAttemptsLeft > 0)
                {
                    ConsoleIO.WriteLogLine("Waiting 5 seconds (" + ReconnectionAttemptsLeft + " attempts left)...");
                    Thread.Sleep(5000);
                    ReconnectionAttemptsLeft--;
                    Program.Restart();
                }
                else if (Settings.interactiveMode)
                {
                    Program.HandleFailure();
                }
            }
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and to leave the server.
        /// </summary>
        private void CommandPrompt()
        {
            try
            {
                while (client.Client.Connected)
                {
                    string text = ConsoleIO.ReadLine();
                    if (ConsoleIO.BasicIO && text.Length > 0 && text[0] == (char)0x00)
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

        /// <summary>
        /// Disconnect the client from the server
        /// </summary>
        public void Disconnect()
        {
            if (handler != null)
            {
                handler.Disconnect();
                handler.Dispose();
            }

            if (cmdprompt != null)
                cmdprompt.Abort();

            Thread.Sleep(1000);

            if (client != null)
                client.Close();
        }

        /// <summary>
        /// Load a new bot
        /// </summary>
        public void BotLoad(ChatBot b, bool init = true)
        {
            b.SetHandler(this);
            if (init)
                b.Initialize();
            if (this.handler != null)
                b.AfterGameJoined();

            bots.Add(b);
        }

        /// <summary>
        /// Unload a bot
        /// </summary>
        public void BotUnLoad(ChatBot b)
        {
            bots.RemoveAll(item => object.ReferenceEquals(item, b));

            // ToList is needed to avoid an InvalidOperationException from modfiying the list while it's being iterated upon.
            var botRegistrations = registeredBotPluginChannels.Where(entry => entry.Value.Contains(b)).ToList();
            foreach (var entry in botRegistrations)
            {
                UnregisterPluginChannel(entry.Key, b);
            }
        }

        /// <summary>
        /// Clear bots
        /// </summary>
        public void BotClear()
        {
            bots.Clear();
        }

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
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
            foreach (ChatBot bot in bots)
                bot.AfterGameJoined();
            player.OnJoin();
        }

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        public void OnRespawn()
        {
            if (terrainAndMovementsRequested)
            {
                terrainAndMovementsEnabled = true;
                terrainAndMovementsRequested = false;
                ConsoleIO.WriteLogLine("Terrain and Movements is now enabled.");
            }

            if (terrainAndMovementsEnabled)
            {
                world.Clear();
            }
        }

        /// <summary>
        /// Get Terrain and Movements status.
        /// </summary>
        public bool GetTerrainEnabled()
        {
            return terrainAndMovementsEnabled;
        }

        /// <summary>
        /// Enable or disable Terrain and Movements.
        /// Please note that Enabling will be deferred until next relog, respawn or world change.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public bool SetTerrainEnabled(bool enabled)
        {
            if (enabled)
            {
                if (!terrainAndMovementsEnabled)
                {
                    terrainAndMovementsRequested = true;
                    return false;
                }
            }
            else
            {
                terrainAndMovementsEnabled = false;
                terrainAndMovementsRequested = false;
                player.locationReceived = false;
                world.Clear();
            }
            return true;
        }

        /// <summary>
        /// Received some text from the server
        /// </summary>
        /// <param name="text">Text received</param>
        /// <param name="isJson">TRUE if the text is JSON-Encoded</param>
        /// <param name="links">Links embedded in text</param>
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
            foreach (ChatBot bot in bots.ToArray())
            {
                try
                {
                    bot.GetText(text);
                    if (bots.Contains(bot))
                        bot.GetText(text, json);
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLineFormatted("§8GetText: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }
        }

        /// <summary>
        /// When connection has been lost
        /// </summary>
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

            foreach (ChatBot bot in bots)
                will_restart |= bot.OnDisconnect(reason, message);

            if (!will_restart)
                Program.HandleFailure();
        }

        /// <summary>
        /// Called ~10 times per second by the protocol handler
        /// </summary>
        public void OnUpdate()
        {
            foreach (var bot in bots.ToArray())
            {
                try
                {
                    bot.Update();
                    bot.ProcessQueuedText();
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLineFormatted("§8Update: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

            foreach (IPlugin plugin in plugins)
            {
                plugin.OnUpdate();
            }

            player.OnUpdate();

        }

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        /// <returns>True if the text was sent with no error</returns>
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

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        public bool SendRespawnPacket()
        {
            return handler.SendRespawnPacket();
        }

        /// <summary>
        /// Triggered when a new player joins the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        /// <param name="name">Name of the player</param>
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

        /// <summary>
        /// Triggered when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        public void OnPlayerLeave(Guid uuid)
        {
            lock (onlinePlayers)
            {
                onlinePlayers.Remove(uuid);
            }
        }

        /// <summary>
        /// Get a set of online player names
        /// </summary>
        /// <returns>Online player names</returns>
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

        /// <summary>
        /// Registers the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to register.</param>
        /// <param name="bot">The bot to register the channel for.</param>
        public void RegisterPluginChannel(string channel, ChatBot bot)
        {
            if (registeredBotPluginChannels.ContainsKey(channel))
            {
                registeredBotPluginChannels[channel].Add(bot);
            }
            else
            {
                List<ChatBot> bots = new List<ChatBot>();
                bots.Add(bot);
                registeredBotPluginChannels[channel] = bots;
                SendPluginChannelMessage("REGISTER", Encoding.UTF8.GetBytes(channel), true);
            }
        }

        /// <summary>
        /// Unregisters the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to unregister.</param>
        /// <param name="bot">The bot to unregister the channel for.</param>
        public void UnregisterPluginChannel(string channel, ChatBot bot)
        {
            if (registeredBotPluginChannels.ContainsKey(channel))
            {
                List<ChatBot> registeredBots = registeredBotPluginChannels[channel];
                registeredBots.RemoveAll(item => object.ReferenceEquals(item, bot));
                if (registeredBots.Count == 0)
                {
                    registeredBotPluginChannels.Remove(channel);
                    SendPluginChannelMessage("UNREGISTER", Encoding.UTF8.GetBytes(channel), true);
                }
            }
        }

        /// <summary>
        /// Sends a plugin channel packet to the server.  See http://wiki.vg/Plugin_channel for more information
        /// about plugin channels.
        /// </summary>
        /// <param name="channel">The channel to send the packet on.</param>
        /// <param name="data">The payload for the packet.</param>
        /// <param name="sendEvenIfNotRegistered">Whether the packet should be sent even if the server or the client hasn't registered it yet.</param>
        /// <returns>Whether the packet was sent: true if it was sent, false if there was a connection error or it wasn't registered.</returns>
        public bool SendPluginChannelMessage(string channel, byte[] data, bool sendEvenIfNotRegistered = false)
        {
            if (!sendEvenIfNotRegistered)
            {
                if (!registeredBotPluginChannels.ContainsKey(channel))
                {
                    return false;
                }
                if (!registeredServerPluginChannels.Contains(channel))
                {
                    return false;
                }
            }
            return handler.SendPluginChannelPacket(channel, data);
        }

        /// <summary>
        /// Called when a plugin channel message was sent from the server.
        /// </summary>
        /// <param name="channel">The channel the message was sent on</param>
        /// <param name="data">The data from the channel</param>
        public void OnPluginChannelMessage(string channel, byte[] data)
        {
            if (channel == "REGISTER")
            {
                string[] channels = Encoding.UTF8.GetString(data).Split('\0');
                foreach (string chan in channels)
                {
                    if (!registeredServerPluginChannels.Contains(chan))
                    {
                        registeredServerPluginChannels.Add(chan);
                    }
                }
            }
            if (channel == "UNREGISTER")
            {
                string[] channels = Encoding.UTF8.GetString(data).Split('\0');
                foreach (string chan in channels)
                {
                    registeredServerPluginChannels.Remove(chan);
                }
            }

            if (registeredBotPluginChannels.ContainsKey(channel))
            {
                foreach (ChatBot bot in registeredBotPluginChannels[channel])
                {
                    bot.OnPluginMessage(channel, data);
                }
            }
        }
    }
}
