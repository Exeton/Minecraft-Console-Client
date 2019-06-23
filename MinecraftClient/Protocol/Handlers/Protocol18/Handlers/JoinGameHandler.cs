using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class JoinGameHandler : IPacketHandler
    {
        IMinecraftComHandler handler;
        DataTypes dataTypes;
        WorldInfo worldInfo;
        int protocolversion;
        public JoinGameHandler(IMinecraftComHandler handler, DataTypes dataTypes, WorldInfo worldInfo, int protocolversion)
        {
            this.handler = handler;
            this.dataTypes = dataTypes;
            this.worldInfo = worldInfo;
            this.protocolversion = protocolversion;
        }
        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {


            handler.OnGameJoined();
            dataTypes.ReadNextInt(data);
            dataTypes.ReadNextByte(data);
            if (protocolversion >= (int)McVersion.V191)
                worldInfo.dimension = dataTypes.ReadNextInt(data);
            else
                worldInfo.dimension = (sbyte)dataTypes.ReadNextByte(data);
            if (protocolversion < (int)McVersion.V114)
                dataTypes.ReadNextByte(data);           // Difficulty - 1.13 and below
            dataTypes.ReadNextByte(data);
            dataTypes.ReadNextString(data);
            if (protocolversion >= (int)McVersion.V114)
                dataTypes.ReadNextVarInt(data);         // View distance - 1.14 and above
            if (protocolversion >= (int)McVersion.V18)
                dataTypes.ReadNextBool(data);           // Reduced debug info - 1.8 and above
            return true;
        }
    }
}
