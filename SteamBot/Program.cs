using System;
using System.Threading;
using SteamKit2;
using SteamTrade;
using System.IO;

namespace SteamBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-help")
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Type -s [file name], to load from a different config file");
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                    if (args[i] == "-s")
                    {
                        string SettingsFile = args[i + 1].Replace("\"", "");
                        if (!SettingsFile.Contains(".json"))
                        {
                            SettingsFile = SettingsFile + ".json";
                        }
                        Load(SettingsFile);
                        return;
                    }
                }
            }
            else
            {
                Load();
                return;
            }
        }

        private static void Load(string file = "settings.json")
        {
            if (System.IO.File.Exists(file))
            {
                Configuration config = Configuration.LoadConfiguration(file);
                Log mainLog = new Log(config.MainLog, null);
                foreach (Configuration.BotInfo info in config.Bots)
                {
                    mainLog.Info("Loading Settings From File " + file + ".");
                    mainLog.Info("Launching Bot " + info.DisplayName + "...");
                    new Thread(() =>
                    {
                        int crashes = 0;
                        while (crashes < 1000)
                        {
                            try
                            {
                                new Bot(info, config.ApiKey, (Bot bot, SteamID sid) =>
                                {

                                    return (SteamBot.UserHandler)System.Activator.CreateInstance(Type.GetType(bot.BotControlClass), new object[] { bot, sid });
                                }, false);

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
                if (file == "settings.json")
                {
                    Console.WriteLine("Configuration File Does not exist. Please rename 'settings-template.json' to 'settings.json' and modify the settings to match your environment");
                }
                else
                {
                    Console.WriteLine("Configuration File Does not exist. Please ensure the file specified exists and modify the settings to match your environment");
                }
            }

        }
    }
}
