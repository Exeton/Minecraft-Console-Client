using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    public class PlayerPositionAndLookHandler : IPacketHandler
    {

        DataTypes dataTypes;
        IPacketSender packetSender;
        IMinecraftComHandler handler;
        int protocolversion;
        public PlayerPositionAndLookHandler(IPacketSender packetSender, DataTypes dataTypes, IMinecraftComHandler handler, int protocolversion)
        {
            this.dataTypes = dataTypes;
            this.packetSender = packetSender;
            this.handler = handler;
            this.protocolversion = protocolversion;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            if (handler.GetTerrainEnabled())
            {
                double x = dataTypes.ReadNextDouble(data);
                double y = dataTypes.ReadNextDouble(data);
                double z = dataTypes.ReadNextDouble(data);
                float yaw = dataTypes.ReadNextFloat(data);
                float pitch = dataTypes.ReadNextFloat(data);
                byte locMask = dataTypes.ReadNextByte(data);

                if (protocolversion >= (int)McVersion.V18)
                {
                    Location location = handler.GetCurrentLocation();
                    location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                    location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                    location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                    handler.UpdateLocation(location, yaw, pitch);
                }
                else handler.UpdateLocation(new Location(x, y, z), yaw, pitch);
            }

            if (protocolversion >= (int)McVersion.V19)
            {
                int teleportID = dataTypes.ReadNextVarInt(data);
                // Teleport confirm packet
                packetSender.SendPacket(PacketOutgoingType.TeleportConfirm, dataTypes.GetVarInt(teleportID));
            }
            return true;
        }
    }
}
