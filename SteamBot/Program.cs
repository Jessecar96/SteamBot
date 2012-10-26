using System;
using System.Threading;

namespace SteamBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.LoadConfiguration("settings.json");
			Log mainLog = new Log (config.MainLog, null);
            //byte counter = 0;
            foreach (Configuration.BotInfo info in config.Bots)
            {
                //Console.WriteLine("--Launching bot " + info.DisplayName +"--");
				mainLog.Info ("Launching Bot " + info.DisplayName + "...");
                new Thread(() =>
                {
                    int crashes = 0;
                    while (crashes < 1000)
                    {
                        try
                        {
                            new Bot(info, config.ApiKey);
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
