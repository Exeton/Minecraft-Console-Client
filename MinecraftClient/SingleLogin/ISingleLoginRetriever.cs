using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.SingleLogin
{
    interface ISingleLoginRetriever
    {

        KeyValuePair<string, string> GetUserAndPass();

    }
}
