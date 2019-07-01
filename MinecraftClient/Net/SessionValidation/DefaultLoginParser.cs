using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Net.SessionValidation
{
    public class DefaultLoginParser : ILoginParser
    {
        public List<KeyValuePair<string, string>> GetLoginPasswordPairs(string text)
        {
            List<KeyValuePair<string, string>> loginInfo = new List<KeyValuePair<string, string>>();
            string[] loginPairs = text.Split(':');

            if (loginPairs.Length % 2 != 0)            
                throw new Exception("There must be an equal number of usernames and passwords");
            
            for (int i = 0; i < loginPairs.Length; i += 2)           
                //Does this trim tab and new line?
                loginInfo.Add(new KeyValuePair<string, string>(loginPairs[i].Trim(), loginPairs[i + 1].Trim()));

            return loginInfo;
        }
    }
}
