using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.MVVM.Client.Session
{
    class SessionVerifier
    {

        public static ProtocolHandler.LoginResult tryAuthenticateSession(out SessionToken session, string usr, string pass)
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

        public static void handleLoginFailure(ProtocolHandler.LoginResult result)
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
            //HandleFailure(failureMessage, false, DisconnectReason.LoginRejected);

        }
    }
}
