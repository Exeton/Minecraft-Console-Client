using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.API
{
    public interface IPlugin
    {
        void OnJoin();
        void OnUpdate();
    }
}
