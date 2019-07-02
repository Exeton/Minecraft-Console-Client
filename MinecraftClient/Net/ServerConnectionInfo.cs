using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.View
{
    public class ServerConnectionInfo
    {
        public string ServerIP = "";
        public ushort ServerPort = 25565;
        public string ServerVersion = "";

        public ServerConnectionInfo(string ip, ushort port)
        {
            this.ServerIP = ip;
            this.ServerPort = port;
        }

    }
}
