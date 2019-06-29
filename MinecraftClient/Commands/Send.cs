using MinecraftClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Send : Command
    {
        public override string CMDName { get { return "send"; } }
        public override string CMDDesc { get { return "send <text>: send a chat message or command."; } }

        TcpClientRetriever tcpClientRetriever;

        public Send(TcpClientRetriever tcpClientRetriever)
        {
            this.tcpClientRetriever = tcpClientRetriever;
        }

        public override string Run(string command, string[] args, string argStr)
        {
            if (args.Length > 0)
            {
                tcpClientRetriever.GetTcpClient().SendText(argStr);
                return "";
            }
            else return CMDDesc;
        }
    }
}
