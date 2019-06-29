using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class CommandHandler
    {
        public Dictionary<string, Command> commands = new Dictionary<string, Command>();

        public CommandHandler addCommand(string name, Command command)
        {
            commands.Add(name.ToLower(), command);
            return this;
        }

        public void runCommand(string cmd)
        {
            runCommand(cmd, new string[] { });
        }

        public void runCommand(string cmd, string[] args)
        {
            cmd = cmd.ToLower();

            Command command;
            if (commands.TryGetValue(cmd, out command))      
                command.Run(null, args);           
            else
                ConsoleIO.WriteLineFormatted("§8Unknown command '" + cmd + "'.");
        }

    }
}
