using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Net.SessionValidation
{
    public interface ILoginParser
    {
        List<KeyValuePair<string, string>> GetLoginPasswordPairs(string text);
    }
}
