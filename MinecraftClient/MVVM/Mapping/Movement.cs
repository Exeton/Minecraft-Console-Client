﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Allows moving through a Minecraft world
    /// </summary>
    public static class Movement
    {
        /* ========= PATHFINDING METHODS ========= */

        /// <summary>
        /// Return a list of possible moves for the player
        /// </summary>
        /// <param name="world">World the player is currently located in</param>
        /// <param name="location">Location the player is currently at</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations</param>
        /// <returns>A list of new locations the player can move to</returns>
        public static IEnumerable<Location> GetAvailableMoves(World world, Location location, bool allowUnsafe = false)
        {
            List<Location> availableMoves = new List<Location>();
            if (IsOnGround(world, location) || IsSwimming(world, location))
            {
                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                    if (CanMove(world, location, dir) && (allowUnsafe || IsSafe(world, location.Move(dir))))
                        availableMoves.Add(location.Move(dir));
            }
            else
            {
                foreach (Direction dir in new []{ Direction.East, Direction.West, Direction.North, Direction.South })
                    if (CanMove(world, location, dir) && IsOnGround(world, location.Move(dir)) && (allowUnsafe || IsSafe(world, location.Move(dir))))
                        availableMoves.Add(location.Move(dir));
                availableMoves.Add(location.Move(Direction.Down));
            }
            return availableMoves;
        }

        /// <summary>
        /// Decompose a single move from a block to another into several steps
        /// </summary>
        /// <remarks>
        /// Allows moving by little steps instead or directly moving between blocks,
        /// which would be rejected by anti-cheat plugins anyway.
        /// </remarks>
        /// <param name="start">Start location</param>
        /// <param name="goal">Destination location</param>
        /// <param name="motionY">Current vertical motion speed</param>
        /// <param name="falling">Specify if performing falling steps</param>
        /// <param name="stepsByBlock">Amount of steps by block</param>
        /// <returns>A list of locations corresponding to the requested steps</returns>
        public static Queue<Location> Move2Steps(Location start, Location goal, ref double motionY, bool falling = false, int stepsByBlock = 8)
        {
            if (stepsByBlock <= 0)
                stepsByBlock = 1;

            //Regular MCC moving algorithm
            motionY = 0; //Reset motion speed
            double totalStepsDouble = start.Distance(goal) * stepsByBlock;
            int totalSteps = (int)Math.Ceiling(totalStepsDouble);
            Location step = (goal - start) / totalSteps;

            if (totalStepsDouble >= 1)
            {
                Queue<Location> movementSteps = new Queue<Location>();
                for (int i = 1; i <= totalSteps; i++)
                    movementSteps.Enqueue(start + step * i);
                return movementSteps;
            }
            else return new Queue<Location>(new[] { goal });            
        }

        /// <summary>
        /// Calculate a path from the start location to the destination location
        /// </summary>
        /// <remarks>
        /// Based on the A* pathfinding algorithm described on Wikipedia
        /// </remarks>
        /// <see href="https://en.wikipedia.org/wiki/A*_search_algorithm#Pseudocode"/>
        /// <param name="allowUnsafe">Allow possible but unsafe locations</param>
        /// <returns>A list of locations, or null if calculation failed</returns>
        public static Queue<Location> CalculatePath(World world, Location start, Location goal, bool allowUnsafe = false)
        {
            Queue<Location> result = null;

            AutoTimeout.Perform(() =>
            {
                HashSet<Location> ClosedSet = new HashSet<Location>(); // The set of locations already evaluated.
                HashSet<Location> OpenSet = new HashSet<Location>(new[] { start });  // The set of tentative nodes to be evaluated, initially containing the start node
                Dictionary<Location, Location> Came_From = new Dictionary<Location, Location>(); // The map of navigated nodes.

                Dictionary<Location, int> g_score = new Dictionary<Location, int>(); //:= map with default value of Infinity
                g_score[start] = 0; // Cost from start along best known path.
                // Estimated total cost from start to goal through y.
                Dictionary<Location, int> f_score = new Dictionary<Location, int>(); //:= map with default value of Infinity
                f_score[start] = (int)start.DistanceSquared(goal); //heuristic_cost_estimate(start, goal)

                while (OpenSet.Count > 0)
                {
                    Location current = //the node in OpenSet having the lowest f_score[] value
                        OpenSet.Select(location => f_score.ContainsKey(location)
                        ? new KeyValuePair<Location, int>(location, f_score[location])
                        : new KeyValuePair<Location, int>(location, int.MaxValue))
                        .OrderBy(pair => pair.Value).First().Key;
                    if (current == goal)
                    { //reconstruct_path(Came_From, goal)
                        List<Location> total_path = new List<Location>(new[] { current });
                        while (Came_From.ContainsKey(current))
                        {
                            current = Came_From[current];
                            total_path.Add(current);
                        }
                        total_path.Reverse();
                        result = new Queue<Location>(total_path);
                    }
                    OpenSet.Remove(current);
                    ClosedSet.Add(current);
                    foreach (Location neighbor in GetAvailableMoves(world, current, allowUnsafe))
                    {
                        if (ClosedSet.Contains(neighbor))
                            continue;		// Ignore the neighbor which is already evaluated.
                        int tentative_g_score = g_score[current] + (int)current.DistanceSquared(neighbor); //dist_between(current,neighbor) // length of this path.
                        if (!OpenSet.Contains(neighbor))	// Discover a new node
                            OpenSet.Add(neighbor);
                        else if (tentative_g_score >= g_score[neighbor])
                            continue;		// This is not a better path.

                        // This path is the best until now. Record it!
                        Came_From[neighbor] = current;
                        g_score[neighbor] = tentative_g_score;
                        f_score[neighbor] = g_score[neighbor] + (int)neighbor.DistanceSquared(goal); //heuristic_cost_estimate(neighbor, goal)
                    }
                }
            }, TimeSpan.FromSeconds(5));

            return result;
        }

        /* ========= LOCATION PROPERTIES ========= */

        /// <summary>
        /// Check if the specified location is on the ground
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location is on the ground</returns>
        public static bool IsOnGround(World world, Location location)
        {
            return world.GetBlock(location.Move(Direction.Down)).Type.IsSolid()
                && (location.Y <= Math.Truncate(location.Y) + 0.0001);
        }

        /// <summary>
        /// Check if the specified location implies swimming
        /// </summary>
        /// <returns>True if the specified location implies swimming</returns>
        public static bool IsSwimming(World world, Location location)
        {
            return world.GetBlock(location).Type.IsLiquid();
        }

        /// <summary>
        /// Check if the specified location is safe
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the destination location won't directly harm the player</returns>
        public static bool IsSafe(World world, Location location)
        {
            return
                //No block that can harm the player
                   !world.GetBlock(location).Type.CanHarmPlayers()
                && !world.GetBlock(location.Move(Direction.Up)).Type.CanHarmPlayers()
                && !world.GetBlock(location.Move(Direction.Down)).Type.CanHarmPlayers()

                //No fall from a too high place
                && (world.GetBlock(location.Move(Direction.Down)).Type.IsSolid()
                     || world.GetBlock(location.Move(Direction.Down, 2)).Type.IsSolid()
                     || world.GetBlock(location.Move(Direction.Down, 3)).Type.IsSolid())

                //Not an underwater location
                && !(world.GetBlock(location.Move(Direction.Up)).Type.IsLiquid());
        }

        /* ========= SIMPLE MOVEMENTS ========= */

        /// <summary>
        /// Check if the player can move in the specified direction
        /// </summary>
        /// <param name="world">World the player is currently located in</param>
        /// <param name="location">Location the player is currently at</param>
        /// <param name="direction">Direction the player is moving to</param>
        /// <returns>True if the player can move in the specified direction</returns>
        public static bool CanMove(World world, Location location, Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                    return !IsOnGround(world, location);
                case Direction.Up:
                    return (IsOnGround(world, location) || IsSwimming(world, location))                        
                        && !world.GetBlock(location.Move(Direction.Up, 2)).Type.IsSolid();
                case Direction.East:
                case Direction.West:
                case Direction.South:
                case Direction.North:
                    return !world.GetBlock(location.Move(direction)).Type.IsSolid()
                        && !world.GetBlock(location.Move(direction).Move(Direction.Up)).Type.IsSolid();
                default:
                    throw new ArgumentException("Unknown direction", "direction");
            }
        }
    }
}
