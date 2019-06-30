using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.API
{
    public interface IPlugin
    {
        void OnJoin();

        /// <summary>
        /// Runs 10 times per second
        /// </summary>
        void OnUpdate();
    }
}
