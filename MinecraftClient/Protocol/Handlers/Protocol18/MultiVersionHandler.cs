using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18.Handlers
{
    class MultiVersionHandler : IPacketHandler
    {

        private List<PacketHandlerInfo> packetHandlerInfos = new List<PacketHandlerInfo>();
        int version;

        public MultiVersionHandler(int version)
        {
            this.version = version;
        }
        public MultiVersionHandler addPacketHandler(IPacketHandler packetHandler, int minimumVersion)
        {
            packetHandlerInfos.Add(new PacketHandlerInfo(packetHandler, minimumVersion));
            return this;
        }


        public bool HandlePacket(PacketIncomingType packetType, List<byte> data)
        {
            //Use better algorthim for getting the packet handler.
            int minimumVersion = -1;
            PacketHandlerInfo closestPacketHandler = null;

            foreach (PacketHandlerInfo packetHandlerInfo in packetHandlerInfos)
            {
                int handlerVersion = packetHandlerInfo.version;
                if (handlerVersion <= version && handlerVersion > minimumVersion)
                {
                    minimumVersion = handlerVersion;
                    closestPacketHandler = packetHandlerInfo;
                }
            }


            return closestPacketHandler.packetHandler.HandlePacket(packetType, data);

        }
    }
}
