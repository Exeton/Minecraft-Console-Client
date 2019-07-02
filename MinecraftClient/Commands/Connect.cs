using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Connect : Command
    {
        public override string CMDName { get { return "connect"; } }
        public override string CMDDesc { get { return "connect <server> [account]: connect to the specified server."; } }

        public override string Run(string command, string[] args, string argStr)
        {
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    //if (!Settings.SetAccount(args[1]))
                    //{
                    //    return "Unknown account '" + args[1] + "'.";
                    //}
                }

                if (Settings.SetServerIP(args[0]))
                {
                    Program.DisconnectAndExit();
                    return "";
                }
                else return "Invalid server IP '" + args[0] + "'.";
            }
            else return CMDDesc;
        }
    }
}
