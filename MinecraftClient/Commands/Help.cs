using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Help : Command
    {
        public override string CMDName { get { return "help"; } }
        public override string CMDDesc { get { return "Shows the help menu"; } }

        public override string Run(McTcpClient handler, string command)
        {
            ConsoleIO.WriteLineFormatted("§8MCC: " + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + new Commands.Reco().CMDDesc);
            ConsoleIO.WriteLineFormatted("§8MCC: " + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + new Commands.Connect().CMDDesc);
            return "";
        }
    }
}
