using MinecraftClient.API;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using MinecraftClient.WinAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static MinecraftClient.ChatBot;

namespace MinecraftClient.Client
{
    public class Client
    {

        public McTcpClient mcTcpClient;
        private static bool useMcVersionOnce = false;

        public void InitializeClient(string usr, string pass)
        {

            SessionToken session;
            ProtocolHandler.LoginResult loginResult = tryAuthenticateSession(out session, usr, pass);

            if (loginResult == ProtocolHandler.LoginResult.Success)      
                startTcpClient(session);        
            else
                handleLoginFailure(loginResult);
        }

        private ProtocolHandler.LoginResult tryAuthenticateSession(out SessionToken session, string usr, string pass)
        {

            session = new SessionToken();

            if (pass == "-")
            {
                ConsoleIO.WriteLineFormatted("§8You chose to run in offline mode.");
                session.PlayerID = "0";
                session.PlayerName = usr;
                return ProtocolHandler.LoginResult.Success;
            }

            if (Settings.SessionCaching == CacheType.None || !SessionCache.Contains(usr.ToLower()))
                return ProtocolHandler.LoginResult.LoginRequired;

            session = SessionCache.Get(usr.ToLower());
            if (ProtocolHandler.GetTokenValidation(session) == ProtocolHandler.LoginResult.Success)
            {
                ConsoleIO.WriteLineFormatted("§8Cached session is still valid for " + session.PlayerName + '.');
                return ProtocolHandler.LoginResult.Success;
            }

            ConsoleIO.WriteLineFormatted("§8Cached session is invalid or expired.");
            //if (Settings.Password == "")
            //RequestPassword();

            Console.WriteLine("Connecting to Minecraft.net...");
            if (ProtocolHandler.GetLogin(usr, pass, out session) == ProtocolHandler.LoginResult.Success && Settings.SessionCaching != CacheType.None)
            {
                SessionCache.Store(usr.ToLower(), session);
                return ProtocolHandler.LoginResult.Success;
            }

            return ProtocolHandler.LoginResult.LoginRequired;
        }

        void startTcpClient(SessionToken session)
        {            
            if (Settings.ConsoleTitle != "")
                Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);

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
                    mcTcpClient = new McTcpClient(session.PlayerName, session.PlayerID, session.ID, protocolversion, Settings.ServerConnectionInfo.ServerIP, Settings.ServerConnectionInfo.ServerPort, forgeInfo);

                    //Update console title
                    if (Settings.ConsoleTitle != "")
                        Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);
                }
                catch (NotSupportedException) { HandleFailure("Cannot connect to the server : This version is not supported !", true); }
            }
            else HandleFailure("Failed to determine server version.", true);
        }

        /// <summary>
        /// Handle fatal errors such as ping failure, login failure, server disconnection, and so on.
        /// Allows AutoRelog to perform on fatal errors, prompt for server version, and offline commands.
        /// </summary>
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
                //if (offlinePrompt == null)
                //{
                //    offlinePrompt = new Thread(new ThreadStart(delegate
                //    {
                //        string command = " ";

                //        ConsoleIO.WriteLineFormatted("Not connected to any server. Use '" + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + "help' for help.");
                //        ConsoleIO.WriteLineFormatted("Or press Enter to exit Minecraft Console Client.");
                //        while ((command = Console.ReadLine().Trim()).Length > 0)
                //        {
                //            if (Settings.internalCmdChar != ' ' && command[0] == Settings.internalCmdChar)
                //                command = command.Substring(1);

                //            CommandHandler.runCommand(command);
                //        }
                //    }));
                //    offlinePrompt.Start();
                //}
            }
            else Program.DisconnectAndExit();
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
            if (result == ProtocolHandler.LoginResult.SSLError && Program.isUsingMono)
            {
                ConsoleIO.WriteLineFormatted("§8It appears that you are using Mono to run this program."
                    + '\n' + "The first time, you have to import HTTPS certificates using:"
                    + '\n' + "mozroots --import --ask-remove");
                return;
            }
            HandleFailure(failureMessage, false, ChatBot.DisconnectReason.LoginRejected);

        }
    }
}
