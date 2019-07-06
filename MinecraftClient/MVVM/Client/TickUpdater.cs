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
            tickLoop.Interval = 1000 / 22;//Tick at slightly faster than 20tps to compensate for System.Timers inaccuracy.
            tickLoop.AutoReset = true;
            tickLoop.Elapsed += TickLoop_Elapsed;
            tickLoop.Start();
        }

        private void TickLoop_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Add compesation code to make sure this runs at 20tps
            UpdateClients();
            UpdatePlugins();
        }

        private void UpdateClients()
        {
            foreach (ClientPool clientPool in clientPools)
                clientPool.Update();
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
