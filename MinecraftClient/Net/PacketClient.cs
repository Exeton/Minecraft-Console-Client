using MinecraftClient.Data;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Proxy;
using MinecraftClient.View;
using Starksoft.Net.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MinecraftClient.Net
{
    class PacketClient
    {

        public ServerConnectionInfo currentServer;
        public TcpClient client;
        private ProxyClientFactory proxyClientFactory;
        public IMinecraftCom communicationHandler;

        public void Update()
        {
            communicationHandler.Update();
        }
        public bool Connect(ServerConnectionInfo serverInfo, Player player, int protocolVersion, ForgeInfo forgeInfo, IMinecraftComHandler minecraftComHandler)
        {

            client = ProxyHandler.startNewTcpClient(serverInfo.ServerIP, serverInfo.ServerPort);
            //client.Connect(serverInfo.ServerIP, serverInfo.ServerPort);
            client.ReceiveBufferSize = 1024 * 1024;

            communicationHandler = ProtocolHandler.GetProtocolHandler(client, protocolVersion, forgeInfo, minecraftComHandler, player);

            return login();
            //send login packets
        }

        private bool login()
        {

            Console.WriteLine("Version is supported.\nLogging in...");
            try
            {
                if (communicationHandler.Login())
                {
                    Thread respawnPacket = new Thread(new ThreadStart(() =>
                    {
                        Thread.Sleep(500);
                        communicationHandler.SendRespawnPacket();
                    }));
                    respawnPacket.Start();
                }
                return true;
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.Message);
                Console.WriteLine("Failed to join this server.");
                Client.Client.HandleFailure();
                return false;
            }

        }

        public void Disconnect()
        {
            //NEEDS TO BE READDED!!!!!!!!!!!!!!!!!!!!!!!!!!!


            //if (handler != null)
            //{
            //    handler.Disconnect();
            //    handler.Dispose();
            //}

            //if (cmdprompt != null)
            //    cmdprompt.Abort();

            Thread.Sleep(1000);

            if (client != null)
                client.Close();
        }





        private readonly List<string> registeredServerPluginChannels = new List<String>();
        /// <summary>
        /// Sends a plugin channel packet to the server.  See http://wiki.vg/Plugin_channel for more information
        /// about plugin channels.
        /// </summary>
        public bool SendPluginChannelMessage(string channel, byte[] data, bool sendEvenIfNotRegistered = false)
        {
            if (!sendEvenIfNotRegistered)
            {
                if (!registeredServerPluginChannels.Contains(channel))
                {
                    return false;
                }
            }
            return false;
            //return handler.SendPluginChannelPacket(channel, data);
        }
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
        }

    }
}
