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
            string SettingsFile = "";
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-help")
                    {
                        WriteLine("Type -s [file name], to load from a different config file");
                        return;
                    }
                    if (args[i] == "-s")
                    {
                        if (args.Length > i + 1 && args[i + 1] != "")
                        {
                            SettingsFile = args[i + 1];
                            if (!SettingsFile.Contains(".json"))
                            {
                                SettingsFile = SettingsFile + ".json";
                            }
                        }
                        else
                        {
                            WriteLine("Incorrect syntax for command '-s'. The correct syntax is '-s [File Name]. Ignoring command.. Ctrl + C to exit.");
                            SettingsFile = "";
                        }
                    }
                    
                }
                MainLoad(SettingsFile);
                return;
            }
            else
                MainLoad();
        }

        private static void MainLoad(string file = "settings.json")
        {
            if (file == "")
                file = "settings.json";

            if (System.IO.File.Exists(file))
            {
                Configuration config = Configuration.LoadConfiguration(file);
                Log mainLog = new Log(config.MainLog, null);
                mainLog.Info("Loading settings from file " + file + ".");
                foreach(Configuration.BotInfo info in config.Bots)
                {
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
                    Console.WriteLine("Configuration file " + file + " does not exist. Please rename 'settings-template.json' to 'settings.json' and modify the settings to match your environment.");
                }
                else
                {
                    Console.WriteLine("Configuration file " + file + " does not exist. Please ensure the file specified exists and modify the settings to match your environment.");
                }
            }
        }

        private static void WriteLine(string message)
        {
            //Simple method to write text to console in green without use of Log, used mainly for help message.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
