using MinecraftClient.Mapping;
using MinecraftClient.MVVM.Client.Session;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace MinecraftClient.Client
{
    public class ClientPool
    {

        List<McTcpClient> mcTcpClients = new List<McTcpClient>();

        public void CreateAndConnectClient(string usr, string pass)
        {
            SessionToken session;
            ProtocolHandler.LoginResult loginResult = SessionVerifier.tryAuthenticateSession(out session, usr, pass);

            if (loginResult == ProtocolHandler.LoginResult.Success)
                mcTcpClients.Add(new McTcpClient(session));
            else
                SessionVerifier.handleLoginFailure(loginResult);
        }

        public void Update()
        {
            foreach (McTcpClient mcTcpClient in mcTcpClients)           
                if (mcTcpClient != null)
                    mcTcpClient.OnUpdate();
            
        }

    }
}
