using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Data;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Look : Command
    {
        public override string CMDName { get { return "look"; } }
        public override string CMDDesc { get { return "look <x y z|yaw pitch|up|down|east|west|north|south>: look at direction or coordinates."; } }

        TcpClientRetriever tcpClientRetriever;

        public Look(TcpClientRetriever tcpClientRetriever)
        {
            this.tcpClientRetriever = tcpClientRetriever;
        }

        public override string Run(string command, string[] args, string argStr)
        {
            McTcpClient handler = tcpClientRetriever.GetTcpClient();

            if (handler.GetTerrainEnabled())
            {
                if (args.Length == 1)
                {
                    string dirStr = args[0].Trim().ToLower();
                    Direction direction = DirectionMethods.FromString(dirStr);
                    if (direction == Direction.None)
                        return "Invalid direction: " + dirStr;

                    handler.UpdateLocation(handler.GetCurrentLocation(), direction);
                    return "Looking " + dirStr;
                }
                else if (args.Length == 2)
                {
                    try
                    {
                        float yaw = Single.Parse(args[0]);
                        float pitch = Single.Parse(args[1]);

                        handler.UpdateLocation(handler.GetCurrentLocation(), yaw, pitch);
                        return String.Format("Looking at YAW: {0} PITCH: {1}", yaw.ToString("0.00"), pitch.ToString("0.00"));
                    }
                    catch (FormatException) { return CMDDesc; }
                }
                else if (args.Length == 3)
                {
                    try
                    {
                        int x = int.Parse(args[0]);
                        int y = int.Parse(args[1]);
                        int z = int.Parse(args[2]);

                        Location block = new Location(x, y, z);
                        handler.UpdateLocation(handler.GetCurrentLocation(), block);

                        return "Looking at " + block;
                    }
                    catch (FormatException) { return CMDDesc; }

                }
                else return CMDDesc;
            }
            else return "Please enable terrainandmovements in config to use this command.";
        }
    }
}
