using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class PacketHandlerInfo
    {

        public IPacketHandler packetHandler;
        public int version;

        public PacketHandlerInfo(IPacketHandler packetHandler, int version)
        {
            this.packetHandler = packetHandler;
            this.version = version;
        }
    }
}
