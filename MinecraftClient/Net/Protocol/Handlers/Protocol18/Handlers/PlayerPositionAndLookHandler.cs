using MinecraftClient.Data;
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
        IPacketReadWriter packetSender;
        IMinecraftComHandler handler;
        int protocolversion;
        Player player;
        public PlayerPositionAndLookHandler(IPacketReadWriter packetSender, DataTypes dataTypes, IMinecraftComHandler handler, Player player, int protocolversion)
        {
            this.dataTypes = dataTypes;
            this.packetSender = packetSender;
            this.handler = handler;
            this.player = player;
            this.protocolversion = protocolversion;
        }

        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {

            double x = dataTypes.ReadNextDouble(data);
            double y = dataTypes.ReadNextDouble(data);
            double z = dataTypes.ReadNextDouble(data);
            float yaw = dataTypes.ReadNextFloat(data);
            float pitch = dataTypes.ReadNextFloat(data);
            byte locMask = dataTypes.ReadNextByte(data);

            if (protocolversion >= (int)McVersion.V18)
            {
                Location location = player.GetCurrentLocation();
                location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                player.UpdateLocation(location, yaw, pitch);
            }
            else player.UpdateLocation(new Location(x, y, z), yaw, pitch);
            

            if (protocolversion >= (int)McVersion.V19)
            {
                int teleportID = dataTypes.ReadNextVarInt(data);
                // Teleport confirm packet
                packetSender.WritePacket(PacketOutgoingType.TeleportConfirm, dataTypes.GetVarInt(teleportID));
            }
            return true;
        }
    }
}
