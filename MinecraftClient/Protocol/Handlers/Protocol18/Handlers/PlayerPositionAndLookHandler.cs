using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class PlayerPositionAndLookHandler : IPacketHandler
    {
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (handler.GetTerrainEnabled())
            {
                double x = dataTypes.ReadNextDouble(packetData);
                double y = dataTypes.ReadNextDouble(packetData);
                double z = dataTypes.ReadNextDouble(packetData);
                float yaw = dataTypes.ReadNextFloat(packetData);
                float pitch = dataTypes.ReadNextFloat(packetData);
                byte locMask = dataTypes.ReadNextByte(packetData);

                if (protocolversion >= MC18Version)
                {
                    Location location = handler.GetCurrentLocation();
                    location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                    location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                    location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                    handler.UpdateLocation(location, yaw, pitch);
                }
                else handler.UpdateLocation(new Location(x, y, z), yaw, pitch);
            }

            if (protocolversion >= MC19Version)
            {
                int teleportID = dataTypes.ReadNextVarInt(packetData);
                // Teleport confirm packet
                SendPacket(PacketOutgoingType.TeleportConfirm, dataTypes.GetVarInt(teleportID));
            }
            return true;
        }
    }
}
