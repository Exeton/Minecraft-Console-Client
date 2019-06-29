using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Help : Command
    {
        CommandHandler commandHandler;
        public override string CMDName { get { return "help"; } }
        public override string CMDDesc { get { return "help <cmdname>: show brief help about a command."; } }

        public Help(CommandHandler commandHandler)
        {
            this.commandHandler = commandHandler;
        }

        public override string Run(string command, string[] args, string argStr)
        {
            string response_msg;
            string availableCmdsMsg = "Available commands: " + String.Join(", ", commandHandler.commands.Keys.ToArray()) + ".For server help, use '" + Settings.internalCmdChar + "send /help' instead.";
            if (args.Length > 0)
            {
                string help_cmdname = args[0];

                if (commandHandler.commands.ContainsKey(help_cmdname))
                {
                    response_msg = commandHandler.commands[help_cmdname].CMDDesc;
                }
                else response_msg = "Unknown command '" + help_cmdname + "'. " + availableCmdsMsg;
            }
            else response_msg = "help <cmdname>. " + availableCmdsMsg;

            ConsoleIO.WriteLine(response_msg);
            return "";
        }
    }
}
