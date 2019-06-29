using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Log : Command
    {
        public override string CMDName { get { return "log"; } }
        public override string CMDDesc { get { return "log <text>: log some text to the console."; } }

        public override string Run(string command, string[] args, string argStr)
        {
            if (args.Length > 0)
            {
                ConsoleIO.WriteLogLine(string.Join(" ", args));
                return "";
            }
            else return CMDDesc;
        }
    }
}
