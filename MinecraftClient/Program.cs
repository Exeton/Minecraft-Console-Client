using System;
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
using MinecraftClient.SingleLogin;
using MinecraftClient.Client;

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
        public static CommandHandler CommandHandler;

        public const string Version = MCHighestVersion;
        public const string MCLowestVersion = "1.4.6";
        public const string MCHighestVersion = "1.14.2";
        public static readonly string BuildInfo = null;

        private static Thread offlinePrompt = null;
        static Thread commandHandler;

        static void Main(string[] args)
        {
            //Only code specific to the Console UI should go in here. For example, the console color settings, etc. 
            Console.WriteLine("Console Client for MC {0} to {1} - v{2} - By ORelio & Contributors", MCLowestVersion, MCHighestVersion, Version);


            CommandHandler = new CommandHandler();
            CommandHandler.addCommand("connect", new Connect());
            CommandHandler.addCommand("exit", new Exit());
            CommandHandler.addCommand("quit", new Exit());
            CommandHandler.addCommand("help", new Help(CommandHandler));

            bool keyboardDebug = (args.Length >= 1) && (args[0] == "--keyboard-debug");
            PrepareConsole(keyboardDebug);


            string login = "";
            string pass = "";
            string usr = "";

            if (Settings.ConsoleTitle != "")           
                Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);
            

            if (Settings.SessionCaching == CacheType.Disk)           
                SessionCache.InitializeDiskCache();

            if ((Settings.SessionCaching == CacheType.None || !SessionCache.Contains(login.ToLower())))
            {
                KeyValuePair<string, string> loginPass = new PromptSingleLoginRetriever().GetUserAndPass();
                login = loginPass.Key;
                usr = login;
                pass = loginPass.Value;
            }


            string rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            List<IPlugin> plugins = PluginLoader.LoadPlugins(rootDir + "/plugins");

            TickUpdater tickUpdater = new TickUpdater();

            ClientPool clientPool = new ClientPool();
            clientPool.createClient().InitializeClient(usr, pass);
            clientPool.createClient().InitializeClient(usr + "1", pass);

            tickUpdater.addClientPool(clientPool);
            tickUpdater.plugins.AddRange(plugins);


            commandHandler = new Thread(new ThreadStart(ConsoleInputHandler));
            commandHandler.Name = "MCC Command prompt";
            commandHandler.Start();
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


        private static void ConsoleInputHandler()
        {
            try
            {
                while (true)
                {
                    string text = ConsoleIO.ReadLine();
                    if (text.Length > 0 && text[0] == (char)0x00)
                    {
                        //Process a request from the GUI
                        string[] command = text.Substring(1).Split((char)0x00);
                        switch (command[0].ToLower())
                        {
                            //case "autocomplete":
                            //    if (command.Length > 1) { ConsoleIO.WriteLine((char)0x00 + "autocomplete" + (char)0x00 + handler.AutoComplete(command[1])); }
                            //    else Console.WriteLine((char)0x00 + "autocomplete" + (char)0x00);
                            //    break;
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
                                    //SendText(text);
                                }
                            }
                            //else SendText(text);
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
        }


        public static void DisconnectAndExit()
        {
            new Thread(new ThreadStart(delegate
            {
                //if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Abort(); offlinePrompt = null; ConsoleIO.Reset(); }
                Environment.Exit(0);
            })).Start();
        }

        public static bool isUsingMono
        {
            get
            {
                return Type.GetType("Mono.Runtime") != null;
            }
        }

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