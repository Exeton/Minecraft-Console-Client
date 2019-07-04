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

        public static KeyValuePair<float, float> GetYawAndPitch(this Direction direction)
        {
            float yaw = 0;
            float pitch = 0;

            switch (direction)
            {
                case Direction.Up:
                    pitch = -90;
                    break;
                case Direction.Down:
                    pitch = 90;
                    break;
                case Direction.East:
                    yaw = 270;
                    break;
                case Direction.West:
                    yaw = 90;
                    break;
                case Direction.North:
                    yaw = 180;
                    break;
                case Direction.South:
                    break;
                default:
                    throw new ArgumentException("Unknown direction", "direction");
            }

            return new KeyValuePair<float, float>(yaw, pitch);
        }


        public static Location ToVector(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                    return new Location(0, -1, 0);
                case Direction.Up:
                    return new Location(0, 1, 0);
                case Direction.East:
                    return new Location(1, 0, 0);
                case Direction.West:
                    return new Location(-1, 0, 0);
                case Direction.South:
                    return new Location(0, 0, 1);
                case Direction.North:
                    return new Location(0, 0, -1);
                default:
                    throw new ArgumentException("Unknown direction", "direction");
            }
        }

    }

}
