using MinecraftClient.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    public class AntiAFK : IPlugin
    {
        private int count;
        private int timeping;

        public AntiAFK(int pingparam)
        {
            count = 0;
            timeping = pingparam;
            if (timeping < 10) { timeping = 10; } //To avoid flooding
        }

        public void OnJoin()
        {

        }

        public void OnUpdate()
        {
            count++;
            if (count == timeping)
            {
                CCAPI.GetTcpClient().SendText("/ping");
                count = 0;
            }
        }
    }
}
