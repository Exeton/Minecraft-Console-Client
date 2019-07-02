using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Set : Command
    {
        public override string CMDName { get { return "set"; } }
        public override string CMDDesc { get { return "set varname=value: set a custom %variable%."; } }
       
        public override string Run(string command, string[] args, string argStr)
        {
            if (args.Length == 0)
                return CMDDesc;


            string[] temp = argStr.Split('=');
            if (temp.Length > 1)
            {
                return "varriables not accepted";
                //return "variable name must be A-Za-z0-9.";
            }
            return CMDDesc;
        }
    }
}
