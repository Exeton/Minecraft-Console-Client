using System;
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

            if (argStr == "on")
            {
                handler.SetTerrainEnabled(true);
                return "Enabling Terrain and Movements on next server login, respawn or world change.";
            }
            else if (argStr == "off")
            {
                handler.SetTerrainEnabled(false);
                return "Disabling Terrain and Movements.";
            }
            else if (handler.GetTerrainEnabled())
            {
                if (args.Length == 1)
                {

                    if (argStr == "get")
                        return handler.GetCurrentLocation().ToString();

                    Direction direction = DirectionMethods.FromString(argStr);

                    if (Movement.CanMove(handler.GetWorld(), handler.GetCurrentLocation(), direction))
                    {
                        handler.player.MoveTo(Movement.Move(handler.GetCurrentLocation(), direction));
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
                        if (handler.player.MoveTo(goal))
                            return "Walking to " + goal;
                        return "Failed to compute path to " + goal;
                    }
                    catch (FormatException) { return CMDDesc; }
                }
                else return CMDDesc;
            }
            else return "Please enable terrainandmovements to use this command.";
        }
    }
}
