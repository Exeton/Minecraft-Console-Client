using MinecraftClient.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace MinecraftClient.Client
{
    class TickUpdater
    {

        List<ClientPool> clientPools = new List<ClientPool>();
        public List<IPlugin> plugins = new List<IPlugin>();
        Timer tickLoop;


        public void addClientPool(ClientPool clientPool)
        {
            clientPools.Add(clientPool);
        }

        public void start()
        {
            tickLoop = new Timer();
            tickLoop.Interval = 1d / 20d;
            tickLoop.AutoReset = true;
            tickLoop.Elapsed += TickLoop_Elapsed;
            tickLoop.Start();
        }

        private void TickLoop_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateClients();
            UpdatePlugins();
        }

        private void UpdateClients()
        {
            foreach (ClientPool clientPool in clientPools)
                foreach (Client client in clientPool.clients)
                    if (client.mcTcpClient != null)
                        client.mcTcpClient.OnUpdate();
        }

        private void UpdatePlugins()
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
                    if (e is System.Threading.ThreadAbortException)
                        throw;
                }
            }
        }

    }
}
