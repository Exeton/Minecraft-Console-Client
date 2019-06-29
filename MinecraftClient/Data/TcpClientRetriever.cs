using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Data
{
    public class TcpClientRetriever
    {

        public McTcpClient tcpClient;

        public McTcpClient GetTcpClient()
        {
            return tcpClient;
        }

    }
}
