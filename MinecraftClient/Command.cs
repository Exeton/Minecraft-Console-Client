using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient
{
    /// <summary>
    /// Represents an internal MCC command: Command name, source code and usage message
    /// To add a new command, inherit from this class while adding the command class to the folder "Commands".
    /// If inheriting from the 'Command' class and placed in the 'Commands' namespace, the command will be
    /// automatically loaded and available in main chat prompt, scripts, remote control and command help.
    /// </summary>

    public abstract class Command
    {

        public abstract string CMDName { get; }

        public abstract string CMDDesc { get; }


        public abstract string Run(string command, string[] args, string argStr);

        public static bool hasArg(string command)
        {
            int first_space = command.IndexOf(' ');
            return (first_space > 0 && first_space < command.Length - 1);
        }
    }
}
