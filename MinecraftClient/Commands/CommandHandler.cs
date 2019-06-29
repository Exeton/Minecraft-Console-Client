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

        public bool runCommand(string cmdAndArgs)
        {
            cmdAndArgs = Settings.ExpandVars(cmdAndArgs);
            string[] argsAndCommand = cmdAndArgs.Split(' ');
            if (argsAndCommand.Length > 1)
            {
                string[] args = new string[argsAndCommand.Length - 1];
                Array.Copy(argsAndCommand, 1, args, 0, args.Length);
                return runCommand(argsAndCommand[0], args);
            }
            else
                return runCommand(argsAndCommand[0], new string[] { });
        }

        public bool runCommand(string cmd, string[] args)
        {
            cmd = cmd.ToLower();

            Command command;
            if (commands.TryGetValue(cmd, out command))
            {
                command.Run(null, args, String.Join(" ", args).Trim());
                return true;
            }      

            ConsoleIO.WriteLineFormatted("Unknown command '" + cmd + "'. Use '" + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + "help' for help.");
            return false;
        }

    }
}
