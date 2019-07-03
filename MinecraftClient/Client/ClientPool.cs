using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace MinecraftClient.Client
{
    class ClientPool
    {

        World world;
        List<Client> clients = new List<Client>();

        Timer tickLoop;

        public ClientPool()
        {
            tickLoop = new Timer();
            tickLoop.Interval = 1d / 20d;
            tickLoop.AutoReset = true;
            tickLoop.Elapsed += TickLoop_Elapsed;
            tickLoop.Start();
        }

        public Client createClient()
        {

            Client client = new Client();
            clients.Add(client);
            
            return client;
        }


        private void TickLoop_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (Client client in clients)
                if (client.mcTcpClient != null)
                    client.mcTcpClient.OnUpdate();
        }

        //Update




    }
}
