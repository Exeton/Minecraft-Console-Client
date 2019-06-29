using MinecraftClient.Protocol.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.SessionValidation
{
    interface ISessionValidator
    {
        SessionToken getSessionToken();
    }
}
