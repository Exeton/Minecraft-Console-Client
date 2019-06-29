using MinecraftClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Script : Command
    {
        public override string CMDName { get { return "script"; } }
        public override string CMDDesc { get { return "script <scriptname>: run a script file."; } }


        TcpClientRetriever tcpClientRetriever;

        public Script(TcpClientRetriever tcpClientRetriever)
        {
            this.tcpClientRetriever = tcpClientRetriever;
        }

        public override string Run(string command, string[] args, string argStr)
        {
            if (hasArg(command))
            {
                tcpClientRetriever.GetTcpClient().BotLoad(new ChatBots.Script(argStr));
                return "";
            }
            else return CMDDesc;
        }
    }
}
