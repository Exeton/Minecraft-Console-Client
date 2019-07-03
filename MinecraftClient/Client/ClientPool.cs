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
        public List<Client> clients = new List<Client>();

        public Client createClient()
        {

            Client client = new Client();
            clients.Add(client);
            
            return client;
        }




        //Update




    }
}
