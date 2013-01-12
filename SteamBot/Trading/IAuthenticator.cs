using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot.Trading
{
    interface IAuthenticator
    {

        Web web { get; set; }
        SteamUser.LoginKeyCallback loginKeyCallback { get; set; }
        BotHandler handler { get; set; }

        string[] Authenticate();
    }
}
