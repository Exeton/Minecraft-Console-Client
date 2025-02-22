﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Data;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Move : Command
    {
        public override string CMDName { get { return "move"; } }
        public override string CMDDesc { get { return "move <on|off|get|up|down|east|west|north|south|x y z>: walk or start walking."; } }

        TcpClientRetriever tcpClientRetriever;
        public Move(TcpClientRetriever tcpClientRetriever)
        {
            this.tcpClientRetriever = tcpClientRetriever;
        }

        public override string Run(string command, string[] args, string argStr)
        {
            McTcpClient handler = tcpClientRetriever.GetTcpClient();

            if (args.Length == 1)
            {

                if (argStr == "get")
                    return handler.player.GetCurrentLocation().ToString();

                Direction direction = DirectionMethods.FromString(argStr);

                if (Movement.CanMove(handler.GetWorld(), handler.player.GetCurrentLocation(), direction))
                {
                    //handler.player.MoveTo(Movement.Move(handler.player.GetCurrentLocation(), direction));
                    return "Moving " + argStr + '.';
                }
                else return "Cannot move in that direction.";
            }
            else if (args.Length == 3)
            {
                try
                {
                    int x = int.Parse(args[0]);
                    int y = int.Parse(args[1]);
                    int z = int.Parse(args[2]);
                    Location goal = new Location(x, y, z);
                    //if (handler.player.MoveTo(goal))
                        //return "Walking to " + goal;
                    return "Failed to compute path to " + goal;
                }
                catch (FormatException) { return CMDDesc; }
            }
            else return CMDDesc;

        }
    }
}
