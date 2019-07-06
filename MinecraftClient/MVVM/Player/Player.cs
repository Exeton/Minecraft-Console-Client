using MinecraftClient.Mapping;
using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinecraftClient.Data
{
    public class Player : IPlayerModel
    {
        public int dimension;
        public object locationLock = new object();
        public Location location;
        private float? yaw;
        private float? pitch;
        public bool locationReceived = false;
        public World world;

        private string username;
        private string uuid;

        public static List<Inventory> inventories = new List<Inventory>();
        private Inventory playerInventory;
        private bool inventoryHandlingEnabled;
        private bool inventoryHandlingRequested = false;

        private double motionY;
        IMinecraftCom handler;

        int ticks = 0;
        Stopwatch stopwatch = new Stopwatch();

        public void SetHandler(IMinecraftCom handler)
        {
            this.handler = handler;
        }

        public Player(World world, String username, string uuid)
        {
            this.world = world;
            this.username = username;
            this.uuid = uuid;
            inventoryHandlingEnabled = Settings.InventoryHandling;
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

        public void OnUpdate()
        {
            UpdateLocation();
            handler.SendLocationUpdate(location, false, null, null);
        }

        private void UpdateLocation()
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();

            double ticksPerSecond = ticks / stopwatch.Elapsed.TotalSeconds;
            ticks++;

            HandleGravity();

            //Check for block intersections

            Location blockPlayerInsideOf = new Location(location.X, Math.Floor(location.Y), location.Z);
            Location blockUnderPlayer = blockPlayerInsideOf.Move(Direction.Down);
            double distAboveBlock = location.Y - Math.Floor(location.Y);
            if (distAboveBlock + motionY < 0 && world.GetBlock(blockUnderPlayer).Type != Material.Air)         //Add swimming check
            {
                location = blockPlayerInsideOf;
            }
            else
            {
                location += (Direction.Up.ToVector() * motionY);
            }

            //if (!Movement.IsOnGround(world, location) && !Movement.IsSwimming(world, location))
            //{
            //    while (!Movement.IsOnGround(world, belowFoots) && belowFoots.Y >= 1)
            //        belowFoots = belowFoots.Move(Direction.Down);
            //    location = Movement.Move2Steps(location, belowFoots, ref motionY, true).Dequeue();
            //}
            //else if (!world.GetBlock(onFoots).Type.IsSolid())
            //{
            //    location = Movement.Move2Steps(location, onFoots, ref motionY, false).Dequeue();
            //    motionY = 0;
            //}
        }


        private void HandleGravity()
        {

            if (Movement.IsOnGround(world, location))
            {
                motionY = 0;
            }
            else
            {
                motionY -= 0.08D;
                motionY *= 0.9800000190734863D;
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
        public bool GetInventoryEnabled()
        {
            return inventoryHandlingEnabled;
        }
        /// <summary>
        /// Enable or disable Inventories.
        /// Please note that Enabling will be deferred until next relog.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public void SetInventoryEnabled(bool enabled)
        {
            if (enabled)
            {
                if (!inventoryHandlingEnabled)
                {
                    inventoryHandlingRequested = true;
                    //Delayed?
                }
            }
            else
            {
                inventoryHandlingEnabled = false;
                inventoryHandlingRequested = false;
                inventories.Clear();
                playerInventory = null;
            }
            //Not delayed
        }

        public string GetUsername() { return username; }
        public string GetUserUUID() { return uuid; }
        public Inventory GetInventory()
        {
            return playerInventory;
        }
        public void OnJoin()
        {
            if (inventoryHandlingRequested)
            {
                inventoryHandlingRequested = false;
                inventoryHandlingEnabled = true;
                ConsoleIO.WriteLogLine("Inventory handling is now enabled.");
            }
        }
        public void onInventoryOpen(Inventory inventory)
        {
            //TODO: Handle Inventory
            if (!Player.inventories.Contains(inventory))
            {
                Player.inventories.Add(inventory);
            }
        }
        public void onInventoryClose(byte inventoryID)
        {
            for (int i = 0; i < Player.inventories.Count; i++)
            {
                Inventory inventory = Player.inventories[i];
                if (inventory == null) continue;
                if (inventory.id == inventoryID)
                {
                    Player.inventories.Remove(inventory);
                    return;
                }
            }
        }
    }
}
