using MinecraftClient.Mapping;
using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Data
{
    interface IPlayer
    {


        string GetUsername();
        string GetUserUUID();

        void OnUpdate();

        Location GetCurrentLocation();
        /// <summary>
        /// Called when the server sets the new location for the player
        /// </summary>
        void UpdateLocation(Location location, float yaw, float pitch);

        void OnJoin();

        bool GetInventoryEnabled();
        void SetInventoryEnabled(bool enabled);

        /// <summary>
        /// Called when an inventory is opened
        /// </summary>
        void onInventoryOpen(Inventory inventory);

        /// <summary>
        /// Called when an inventory is closed
        /// </summary>
        void onInventoryClose(byte inventoryID);
        Inventory GetInventory();
    }
}
