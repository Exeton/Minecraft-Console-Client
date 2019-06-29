using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a unit movement in the world
    /// </summary>
    /// <see href="http://minecraft.gamepedia.com/Coordinates"/>
    public enum Direction
    {
        South = 0,
        West = 1,
        North = 2,
        East = 3,
        Up = 4,
        Down = 5,
        None = 6
    }

    static class DirectionMethods
    {

        public static Direction FromString(string str)
        {
            switch (str)
            {
                case "up": return Direction.Up;
                case "down": return Direction.Down;
                case "east": return Direction.East;
                case "west": return Direction.West;
                case "north": return Direction.North;
                case "south": return Direction.South;
            }

            return Direction.None;
        }
    }

}
