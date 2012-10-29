using System;
using System.Threading;
using SteamKit2;

namespace SteamBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.LoadConfiguration("settings.json");
            Log mainLog = new Log (config.MainLog, null);
            foreach (Configuration.BotInfo info in config.Bots)
            {
                mainLog.Info ("Launching Bot " + info.DisplayName + "...");
                new Thread(() =>
                {
                    int crashes = 0;
                    while (crashes < 1000)
                    {
                        try
                        {
                            new Bot(info, config.ApiKey, (Bot bot, SteamID sid) => 
                            {
                                return new SimpleUserHandler(bot, sid);
                            }, true);
                        }
                        catch (Exception e)
                        {
                            mainLog.Error ("Error With Bot: "+e);
                            crashes++;
                        }
                    }
                }).Start();
                Thread.Sleep(5000);
            }
        }
    }
}
