using MinecraftClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Respawn : Command
    {
        public override string CMDName { get { return "respawn"; } }
        public override string CMDDesc { get { return "respawn: Use this to respawn if you are dead."; } }

        TcpClientRetriever tcpClientRetriever;

        public Respawn(TcpClientRetriever tcpClientRetriever)
        {
            this.tcpClientRetriever = tcpClientRetriever;
        }

        public override string Run(string command, string[] args, string argStr)
        {
            tcpClientRetriever.GetTcpClient().SendRespawnPacket();
            return "You have respawned.";
        }
    }
}
