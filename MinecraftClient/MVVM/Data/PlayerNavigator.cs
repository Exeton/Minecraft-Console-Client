using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Data
{
    class PlayerNavigator
    {

        //Refactor into player controler class
        private Queue<Location> steps;
        private Queue<Location> path;
        Player player;

        public bool MoveTo(Location location, bool allowUnsafe = false)
        {
            lock (player.locationLock)
            {

                if (Movement.GetAvailableMoves(player.world, location, allowUnsafe).Contains(location))
                    path = new Queue<Location>(new[] { location });
                else path = Movement.CalculatePath(player.world, location, location, allowUnsafe);
                return path != null;
            }
        }

        public void OnUpdate()
        {
        //    lock (locationLock)
        //    {
        //        for (int i = 0; i < 2; i++) //Needs to run at 20 tps; MCC runs at 10 tps
        //        {
        //            if (yaw == null || pitch == null)
        //            {
        //                if (steps != null && steps.Count > 0)
        //                {
        //                    location = steps.Dequeue();
        //                }
        //                else if (path != null && path.Count > 0)
        //                {
        //                    Location next = path.Dequeue();
        //                    steps = Movement.Move2Steps(location, next, ref motionY);
        //                    UpdateLocation(location, next + new Location(0, 1, 0)); // Update yaw and pitch to look at next step
        //                }
        //            }               

        //            handler.SendLocationUpdate(location, Movement.IsOnGround(world, location), yaw, pitch);
        //        }
        //        // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
        //        // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
        //        yaw = null;
        //        pitch = null;
        //    }
        }
    }
}
