using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.SingleLogin
{
    class PromptSingleLoginRetriever : ISingleLoginRetriever
    {
        public KeyValuePair<string, string> GetUserAndPass()
        {

            string login;
            string pass;

            Console.Write("Login : ");
            login = Console.ReadLine();
            if (login == "")
                login = "N00bBot";



            Console.Write("Password : ");
            pass = ConsoleIO.ReadPassword();//May need to be readline for the GUI
            if (pass == "")
                pass = "-";

            return new KeyValuePair<string, string>(login, pass);
        }
    }
}
