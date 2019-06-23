using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    public class ConnectionInfo
    {
        public int compressionThreshold;
        public SocketWrapper socketWrapper;


        public ConnectionInfo(SocketWrapper socketWrapper, int compressionThreshold)
        {
            this.compressionThreshold = compressionThreshold;
            this.socketWrapper = socketWrapper;
        }

    }
}
