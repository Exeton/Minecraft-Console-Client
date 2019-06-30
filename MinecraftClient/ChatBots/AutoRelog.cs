//using MinecraftClient.API;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using static MinecraftClient.ChatBot;

//namespace MinecraftClient.ChatBots
//{
//    /// <summary>
//    /// This bot automatically re-join the server if kick message contains predefined string (Server is restarting ...)
//    /// </summary>
//    public class AutoRelog : IPlugin
//    {
//        private string[] dictionary = new string[0];
//        private int attempts;
//        private int delay;

//        /// <summary>
//        /// This bot automatically re-join the server if kick message contains predefined string
//        /// </summary>
//        /// <param name="DelayBeforeRelog">Delay before re-joining the server (in seconds)</param>
//        /// <param name="retries">Number of retries if connection fails (-1 = infinite)</param>
//        public AutoRelog(int DelayBeforeRelog, int retries)
//        {
//            attempts = retries;
//            if (attempts == -1) { attempts = int.MaxValue; }
//            McTcpClient.ReconnectionAttemptsLeft = attempts;
//            delay = DelayBeforeRelog;
//            if (delay < 1) { delay = 1; }
//            if (Settings.DebugMessages)
//                Console.WriteLine("Launching with " + attempts + " reconnection attempts");


//            if (System.IO.File.Exists(Settings.AutoRelog_KickMessagesFile))
//            {
//                if (Settings.DebugMessages)
//                    Console.WriteLine("Loading messages from file: " + Settings.AutoRelog_KickMessagesFile);

//                dictionary = System.IO.File.ReadAllLines(Settings.AutoRelog_KickMessagesFile);

//                for (int i = 0; i < dictionary.Length; i++)
//                {
//                    if (Settings.DebugMessages)
//                        Console.WriteLine("  Loaded message: " + dictionary[i]);
//                    dictionary[i] = dictionary[i].ToLower();
//                }
//            }
//            else
//            {
//                Console.WriteLine("File not found: " + Settings.AutoRelog_KickMessagesFile);

//                if (Settings.DebugMessages)
//                    Console.WriteLine("  Current directory was: " + System.IO.Directory.GetCurrentDirectory());
//            }
//        }


//        public override bool OnDisconnect(DisconnectReason reason, string message)
//        {
//            message = GetVerbatim(message);
//            string comp = message.ToLower();

//            if (Settings.DebugMessages)
//                Console.WriteLine("Got disconnected with message: " + message);

//            foreach (string msg in dictionary)
//            {
//                if (comp.Contains(msg))
//                {
//                    if (Settings.DebugMessages)
//                        Console.WriteLine("Message contains '" + msg + "'. Reconnecting.");

//                    Console.WriteLine("Waiting " + delay + " seconds before reconnecting...");
//                    System.Threading.Thread.Sleep(delay * 1000);
//                    McTcpClient.ReconnectionAttemptsLeft = attempts;
//                    ReconnectToTheServer();
//                    return true;
//                }
//            }

//            if (Settings.DebugMessages)
//                Console.WriteLine("Message not containing any defined keywords. Ignoring.");

//            return false;
//        }

//        public static bool OnDisconnectStatic(DisconnectReason reason, string message)
//        {
//            if (Settings.AutoRelog_Enabled)
//            {
//                AutoRelog bot = new AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries);
//                bot.Initialize();
//                return bot.OnDisconnect(reason, message);
//            }
//            return false;
//        }

//        public void OnJoin()
//        {
//            throw new NotImplementedException();
//        }

//        public void OnText(string text)
//        {
//            throw new NotImplementedException();
//        }

//        public void OnUpdate()
//        {
//            throw new NotImplementedException();
//        }

//        void IPlugin.OnDisconnect(DisconnectReason disconnectReason, string message)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
