using System;

namespace SteamBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BotManager manager = new BotManager();

            var loadedOk = manager.LoadConfiguration("settings.json");

            if (!loadedOk) 
            {
                Console.WriteLine(
                    "Configuration file Does not exist or is corrupt. Please rename 'settings-template.json' to 'settings.json' and modify the settings to match your environment");
                Console.Write("Press Enter to exit...");
                Console.ReadLine();
            }
            else
            {
                var startedOk = manager.StartBots();

                if (!startedOk)
                {
                    Console.WriteLine(
                        "Error starting the bots because either the configuration was bad or because the log file was not opened.");
                    Console.Write("Press Enter to exit...");
                    Console.ReadLine();
                }
            }
        }
    }
}
