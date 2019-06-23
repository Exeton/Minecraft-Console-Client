using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class JoinGameHandler : IPacketHandler
    {
        DataTypes dataTypes;

        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            handler.OnGameJoined();
            dataTypes.ReadNextInt(data);
            dataTypes.ReadNextByte(data);
            if (protocolversion >= MC191Version)
                this.currentDimension = dataTypes.ReadNextInt(data);
            else
                this.currentDimension = (sbyte)dataTypes.ReadNextByte(data);
            if (protocolversion < MC114Version)
                dataTypes.ReadNextByte(data);           // Difficulty - 1.13 and below
            dataTypes.ReadNextByte(data);
            dataTypes.ReadNextString(data);
            if (protocolversion >= MC114Version)
                dataTypes.ReadNextVarInt(data);         // View distance - 1.14 and above
            if (protocolversion >= MC18Version)
                dataTypes.ReadNextBool(data);           // Reduced debug info - 1.8 and above
            return true;
        }
    }
}
