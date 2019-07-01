using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Exit : Command
    {
        public override string CMDName { get { return "exit"; } }
        public override string CMDDesc { get { return "exit: disconnect from the server."; } }

        public override string Run(string command, string[] args, string argStr)
        {
            Program.DisconnectAndExit();
            return "";
        }
    }
}
