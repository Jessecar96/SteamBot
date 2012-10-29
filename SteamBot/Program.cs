using System;
using System.Threading;

namespace SteamBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (System.IO.File.Exists("settings.json"))
            {
                Configuration config = Configuration.LoadConfiguration("settings.json");
                Log mainLog = new Log(config.MainLog, null);
                //byte counter = 0;
                foreach (Configuration.BotInfo info in config.Bots)
                {
                    //Console.WriteLine("--Launching bot " + info.DisplayName +"--");
                    mainLog.Info("Launching Bot " + info.DisplayName + "...");
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
                                mainLog.Error("Error With Bot: " + e);
                                crashes++;
                            }
                        }
                    }).Start();
                    Thread.Sleep(5000);
                }
            }
            else
            {
                Console.WriteLine("Configuration File Does not exist. Please rename 'settings-template.json' to 'settings.json' and modify the settings to match your environment");
            }
        }
    }
}
