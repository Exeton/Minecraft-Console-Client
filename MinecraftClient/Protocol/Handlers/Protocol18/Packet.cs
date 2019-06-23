using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Protocol18
{
    public class Packet
    {
        public int id;
        public List<byte> data = new List<byte>();

    }
}
