using System;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
using SteamKit2;
using SteamTrade;
using SteamKit2.Internal;


namespace SteamBot
{
    public class Bot1Queue
    {
        public static SteamID[] steamidInQueue = new SteamID[200];
        public static int peopleInQueue = 0;
        public static int postionInQueue = 0;
        public static int requestTrade = 0;
        public static bool locked =false;
        public static int success =0;
        public static long requesttradefiletime=0;
        
        
    }
}
