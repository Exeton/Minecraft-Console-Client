using MinecraftClient.Client;
using MinecraftClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.API
{
    public class ConsoleAPI
    {
        public static ClientPool GetClientPool()
        {
            return Program.ClientPool;
        }
    }
}
