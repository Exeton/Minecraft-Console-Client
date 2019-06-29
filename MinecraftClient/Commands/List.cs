using MinecraftClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class List : Command
    {
        public override string CMDName { get { return "list"; } }
        public override string CMDDesc { get { return "list: get the player list."; } }

        TcpClientRetriever tcpClientRetriever;
        public List(TcpClientRetriever tcpClientRetriever)
        {
            this.tcpClientRetriever = tcpClientRetriever;
        }

        public override string Run(string command, string[] args, string argStr)
        {
            return "PlayerList: " + string.Join(", ", tcpClientRetriever.GetTcpClient().GetOnlinePlayers());
        }
    }
}

