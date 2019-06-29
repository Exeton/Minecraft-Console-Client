using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Abstract outgoing packet numbering
    /// </summary>
    public enum McVersion
    {
        V17 = 40,// I created this
        V18 = 47,
        V19 = 107,
        V191 = 108,
        V110 = 210,
        V1112 = 316,
        V112 = 335,
        V1121 = 338,
        V1122 = 340,
        V113 = 393,
        V114 = 477,
        V1142 = 485
    }
}
