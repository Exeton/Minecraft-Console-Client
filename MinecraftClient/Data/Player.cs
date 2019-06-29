using MinecraftClient.Mapping;
using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Data
{
    public class Player
    {
        public int dimension;
        public object locationLock = new object();
        public Location location;
        private float? yaw;
        private float? pitch;
        public bool locationReceived = false;
        World world;

        private double motionY;
        //Refactor into player controler class
        private Queue<Location> steps;
        private Queue<Location> path;

        IMinecraftCom handler;

        public Player(World world, IMinecraftCom handler)
        {
            this.world = world;
        }

        public Location GetCurrentLocation()
        {
            return location;
        }

        /// <summary>
        /// Move to the specified location
        /// </summary>
        /// <param name="location">Location to reach</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations</param>
        /// <returns>True if a path has been found</returns>
        public bool MoveTo(Location location, bool allowUnsafe = false)
        {
            lock (locationLock)
            {
                if (Movement.GetAvailableMoves(world, location, allowUnsafe).Contains(location))
                    path = new Queue<Location>(new[] { location });
                else path = Movement.CalculatePath(world, location, location, allowUnsafe);
                return path != null;
            }
        }

        public void OnUpdate()
        {
            if (McTcpClient.terrainAndMovementsEnabled && locationReceived)
            {
                lock (locationLock)
                {
                    for (int i = 0; i < 2; i++) //Needs to run at 20 tps; MCC runs at 10 tps
                    {
                        if (yaw == null || pitch == null)
                        {
                            if (steps != null && steps.Count > 0)
                            {
                                location = steps.Dequeue();
                            }
                            else if (path != null && path.Count > 0)
                            {
                                Location next = path.Dequeue();
                                steps = Movement.Move2Steps(location, next, ref motionY);
                                UpdateLocation(location, next + new Location(0, 1, 0)); // Update yaw and pitch to look at next step
                            }
                            else
                            {
                                location = Movement.HandleGravity(world, location, ref motionY);
                            }
                        }
                        handler.SendLocationUpdate(location, Movement.IsOnGround(world, location), yaw, pitch);
                    }
                    // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
                    // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
                    yaw = null;
                    pitch = null;
                }
            }
        }
        public void UpdateLocation(Location location, bool relative)
        {
            lock (locationLock)
            {
                if (relative)
                {
                    this.location += location;
                }
                else this.location = location;
                locationReceived = true;
            }
        }
        public void UpdateLocation(Location location, float yaw, float pitch)
        {
            this.yaw = yaw;
            this.pitch = pitch;
            UpdateLocation(location, false);
        }
        public void UpdateLocation(Location location, Location targetBlock)
        {
            //0.5 is subtracted from the x and z coords so the player looks at the center of the block.
            double dx = targetBlock.X - (location.X - 0.5);
            double dy = targetBlock.Y - (location.Y + 1);
            double dz = targetBlock.Z - (location.Z - 0.5);

            double r = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            float yaw = Convert.ToSingle(-Math.Atan2(dx, dz) / Math.PI * 180);
            float pitch = Convert.ToSingle(-Math.Asin(dy / r) / Math.PI * 180);
            if (yaw < 0) yaw += 360;

            UpdateLocation(location, yaw, pitch);
        }
        public void UpdateLocation(Location location, Direction direction)
        {
            KeyValuePair<float, float> yawAndPitch = direction.GetYawAndPitch();
            UpdateLocation(location, yawAndPitch.Key, yawAndPitch.Value);
        }
    }
}
