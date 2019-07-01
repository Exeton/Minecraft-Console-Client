﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol;
using System.Reflection;
using System.Threading;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using MinecraftClient.WinAPI;
using MinecraftClient.Commands;
using MinecraftClient.API;
using System.IO;
using static MinecraftClient.ChatBot;

namespace MinecraftClient
{
    /// <summary>
    /// Minecraft Console Client by ORelio and Contributors (c) 2012-2019.
    /// Allows to connect to any Minecraft server, send and receive text, automated scripts.
    /// This source code is released under the CDDL 1.0 License.
    /// </summary>
    /// <remarks>
    /// Typical steps to update MCC for a new Minecraft version
    ///  - Implement protocol changes (see Protocol18.cs)
    ///  - Handle new block types and states (see Material.cs)
    ///  - Mark new version as handled (see ProtocolHandler.cs)
    ///  - Update MCHighestVersion field below (for versionning)
    /// </remarks>
    static class Program
    {
        public static McTcpClient Client;
        public static CommandHandler CommandHandler;
        public static string[] startupargs;

        public const string Version = MCHighestVersion;
        public const string MCLowestVersion = "1.4.6";
        public const string MCHighestVersion = "1.14.2";
        public static readonly string BuildInfo = null;

        private static Thread offlinePrompt = null;
        private static bool useMcVersionOnce = false;

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("Console Client for MC {0} to {1} - v{2} - By ORelio & Contributors", MCLowestVersion, MCHighestVersion, Version);

            CommandHandler = new CommandHandler();
            CommandHandler.addCommand("connect", new Connect());
            CommandHandler.addCommand("exit", new Exit());
            CommandHandler.addCommand("quit", new Exit());
            CommandHandler.addCommand("help", new Help(CommandHandler));

            bool keyboardDebug = (args.Length >= 1) && (args[0] == "--keyboard-debug");
            PrepareConsole(keyboardDebug);

            if (Settings.ConsoleTitle != "")
            {
                Settings.Username = "New Window";
                Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);
            }

            if (Settings.SessionCaching == CacheType.Disk)           
                SessionCache.InitializeDiskCache();        

            promptLoginInfo();
            startupargs = args;

            InitializeClient();
        }

        static void PrepareConsole(bool keyboardDebug)
        {
            if (BuildInfo != null)
            {
                ConsoleIO.WriteLineFormatted("§8" + BuildInfo);
            }

            if (keyboardDebug)
            {
                Console.WriteLine("Keyboard debug mode: Press any key to display info");
                ConsoleIO.DebugReadInput();
            }

            ConsoleIO.LogPrefix = "§8[MCC] ";

            //Take advantage of Windows 10 / Mac / Linux UTF-8 console
            if (isUsingMono || WindowsVersion.WinMajorVersion >= 10)
            {
                Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
            }
        }
        
        static void promptLoginInfo()
        {
            if (Settings.Login == "")
            {
                Console.Write("Login : ");
                Settings.Login = Console.ReadLine();
            }
            if (Settings.Password == "" && (Settings.SessionCaching == CacheType.None || !SessionCache.Contains(Settings.Login.ToLower())))
            {
                RequestPassword();
            }
        }

        /// <summary>
        /// Reduest user to submit password.
        /// </summary>
        private static void RequestPassword()
        {
            Console.Write("Password : ");
            Settings.Password = ConsoleIO.ReadPassword();//May need to be readline for the GUI
            if (Settings.Password == "") { Settings.Password = "-"; }
        }

        /// <summary>
        /// Start a new Client
        /// </summary>
        private static void InitializeClient()
        {
            SessionToken session = new SessionToken();
            ProtocolHandler.LoginResult loginResult = tryAuthenticateSession(session);

            if (loginResult == ProtocolHandler.LoginResult.Success)
            {
                string rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                List<IPlugin> plugins = PluginLoader.LoadPlugins(rootDir + "/plugins");
                startTcpClient(session, plugins);
            }
            else
                handleLoginFailure(loginResult);
        }
        private static ProtocolHandler.LoginResult tryAuthenticateSession(SessionToken session)
        {
            if (Settings.Password == "-")
            {
                ConsoleIO.WriteLineFormatted("§8You chose to run in offline mode.");
                session.PlayerID = "0";
                session.PlayerName = Settings.Login;
                return ProtocolHandler.LoginResult.Success;
            }

            if (Settings.SessionCaching == CacheType.None || !SessionCache.Contains(Settings.Login.ToLower()))
                return ProtocolHandler.LoginResult.LoginRequired;

            session = SessionCache.Get(Settings.Login.ToLower());
            if (ProtocolHandler.GetTokenValidation(session) == ProtocolHandler.LoginResult.Success)
            {
                ConsoleIO.WriteLineFormatted("§8Cached session is still valid for " + session.PlayerName + '.');
                return ProtocolHandler.LoginResult.Success;
            }

            ConsoleIO.WriteLineFormatted("§8Cached session is invalid or expired.");
            if (Settings.Password == "")
                RequestPassword();

            Console.WriteLine("Connecting to Minecraft.net...");
            if (ProtocolHandler.GetLogin(Settings.Login, Settings.Password, out session) == ProtocolHandler.LoginResult.Success && Settings.SessionCaching != CacheType.None)
            {
                SessionCache.Store(Settings.Login.ToLower(), session);
                return ProtocolHandler.LoginResult.Success;
            }
            
            return ProtocolHandler.LoginResult.LoginRequired;
        }
        static void startTcpClient(SessionToken session, List<IPlugin> plugins)
        {
            Settings.Username = session.PlayerName;

            if (Settings.ConsoleTitle != "")
                Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);

            if (Settings.playerHeadAsIcon)
                ConsoleIcon.setPlayerIconAsync(Settings.Username);

            if (Settings.DebugMessages)
                Console.WriteLine("Success. (session ID: " + session.ID + ')');

            //ProtocolHandler.RealmsListWorlds(Settings.Username, PlayerID, sessionID); //TODO REMOVE

            if (Settings.ServerConnectionInfo.ServerIP == "")
            {
                Console.Write("Server IP : ");
                Settings.SetServerIP(Console.ReadLine());
            }

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

                if (useMcVersionOnce)
                {
                    useMcVersionOnce = false;
                    Settings.ServerConnectionInfo.ServerVersion = "";
                }
            }

            if (protocolversion == 0)
            {
                Console.WriteLine("Retrieving Server Info...");
                if (!ProtocolHandler.GetServerInfo(Settings.ServerConnectionInfo.ServerIP, Settings.ServerConnectionInfo.ServerPort, ref protocolversion, ref forgeInfo))
                {
                    HandleFailure("Failed to ping this IP.", true, DisconnectReason.ConnectionLost);
                    return;
                }
            }

            if (protocolversion != 0)
            {
                try
                {
                    Client = new McTcpClient(session.PlayerName, session.PlayerID, session.ID, protocolversion, forgeInfo, Settings.ServerConnectionInfo.ServerIP, Settings.ServerConnectionInfo.ServerPort, plugins);

                    //Update console title
                    if (Settings.ConsoleTitle != "")
                        Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);
                }
                catch (NotSupportedException) { HandleFailure("Cannot connect to the server : This version is not supported !", true); }
            }
            else HandleFailure("Failed to determine server version.", true);
        }
        static void handleLoginFailure(ProtocolHandler.LoginResult result)
        {
                string failureMessage = "Minecraft Login failed : ";
                switch (result)
                {
                    case ProtocolHandler.LoginResult.AccountMigrated: failureMessage += "Account migrated, use e-mail as username."; break;
                    case ProtocolHandler.LoginResult.ServiceUnavailable: failureMessage += "Login servers are unavailable. Please try again later."; break;
                    case ProtocolHandler.LoginResult.WrongPassword: failureMessage += "Incorrect password, blacklisted IP or too many logins."; break;
                    case ProtocolHandler.LoginResult.InvalidResponse: failureMessage += "Invalid server response."; break;
                    case ProtocolHandler.LoginResult.NotPremium: failureMessage += "User not premium."; break;
                    case ProtocolHandler.LoginResult.OtherError: failureMessage += "Network error."; break;
                    case ProtocolHandler.LoginResult.SSLError: failureMessage += "SSL Error."; break;
                    default: failureMessage += "Unknown Error."; break;
                }
                if (result == ProtocolHandler.LoginResult.SSLError && isUsingMono)
                {
                    ConsoleIO.WriteLineFormatted("§8It appears that you are using Mono to run this program."
                        + '\n' + "The first time, you have to import HTTPS certificates using:"
                        + '\n' + "mozroots --import --ask-remove");
                    return;
                }
                HandleFailure(failureMessage, false, ChatBot.DisconnectReason.LoginRejected);
            
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>
        public static void Exit()
        {
            new Thread(new ThreadStart(delegate
            {
                if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Abort(); offlinePrompt = null; ConsoleIO.Reset(); }
                if (Settings.playerHeadAsIcon) { ConsoleIcon.revertToCMDIcon(); }
                Environment.Exit(0);
            })).Start();
        }

        /// <summary>
        /// Handle fatal errors such as ping failure, login failure, server disconnection, and so on.
        /// Allows AutoRelog to perform on fatal errors, prompt for server version, and offline commands.
        /// </summary>
        /// <param name="errorMessage">Error message to display and optionally pass to AutoRelog bot</param>
        /// <param name="versionError">Specify if the error is related to an incompatible or unkown server version</param>
        /// <param name="disconnectReason">If set, the error message will be processed by the AutoRelog bot</param>
        public static void HandleFailure(string errorMessage = null, bool versionError = false, DisconnectReason? disconnectReason = null)
        {
            if (!String.IsNullOrEmpty(errorMessage))
            {
                ConsoleIO.Reset();
                while (Console.KeyAvailable)
                    Console.ReadKey(true);
                Console.WriteLine(errorMessage);

                if (disconnectReason.HasValue)
                {
                    //if (ChatBots.AutoRelog.OnDisconnectStatic(disconnectReason.Value, errorMessage))
                        //return; //AutoRelog is triggering a restart of the client
                }
            }

            if (Settings.interactiveMode && !versionError)
            {
                if (offlinePrompt == null)
                {
                    offlinePrompt = new Thread(new ThreadStart(delegate
                    {
                        string command = " ";
                        ConsoleIO.WriteLineFormatted("Not connected to any server. Use '" + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + "help' for help.");
                        ConsoleIO.WriteLineFormatted("Or press Enter to exit Minecraft Console Client.");
                        while ((command = Console.ReadLine().Trim()).Length > 0)
                        {
                            if (Settings.internalCmdChar != ' ' && command[0] == Settings.internalCmdChar)
                                command = command.Substring(1);

                            CommandHandler.runCommand(command);                                                   
                        }
                    }));
                    offlinePrompt.Start();
                }
            }
            else Exit();
        }

        /// <summary>
        /// Detect if the user is running Minecraft Console Client through Mono
        /// </summary>
        public static bool isUsingMono
        {
            get
            {
                return Type.GetType("Mono.Runtime") != null;
            }
        }

        /// <summary>
        /// Enumerate types in namespace through reflection
        /// </summary>
        /// <param name="nameSpace">Namespace to process</param>
        /// <param name="assembly">Assembly to use. Default is Assembly.GetExecutingAssembly()</param>
        /// <returns></returns>
        public static Type[] GetTypesInNamespace(string nameSpace, Assembly assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetExecutingAssembly(); }
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

        /// <summary>
        /// Static initialization of build information, read from assembly information
        /// </summary>
        static Program()
        {
            AssemblyConfigurationAttribute attribute
             = typeof(Program)
                .Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
                .FirstOrDefault() as AssemblyConfigurationAttribute;
            if (attribute != null)
                BuildInfo = attribute.Configuration;
        }
    }
}