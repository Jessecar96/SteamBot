using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SteamBot
{
    public class Configuration
    {
        public static Configuration LoadConfiguration (string filename)
        {
            TextReader reader = new StreamReader(filename);
            string json = reader.ReadToEnd();
            reader.Close();

            Configuration config =  JsonConvert.DeserializeObject<Configuration>(json);

            config.Admins = config.Admins ?? new ulong[0];

            // merge bot-specific admins with global admins
            foreach (BotInfo bot in config.Bots)
            {
                if (bot.Admins == null)
                {
                    bot.Admins = new ulong[config.Admins.Length];
                    Array.Copy(config.Admins, bot.Admins, config.Admins.Length);
                }
                else
                {
                    bot.Admins = bot.Admins.Concat(config.Admins).ToArray();
                }
            }

            return config;
        }

        public ulong[] Admins { get; set; }
        public BotInfo[] Bots { get; set; }
        public string ApiKey { get; set; }
        public string MainLog { get; set; }

        public class BotInfo
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string DisplayName { get; set; }
            public string ChatResponse { get; set; }
            public string LogFile { get; set; }
            public string BotControlClass { get; set; }
            public int MaximumTradeTime { get; set; }
            public int MaximumActionGap { get; set; }
            public string DisplayNamePrefix { get; set; }
            public int TradePollingInterval { get; set; }
            public string LogLevel { get; set; }
            public ulong[] Admins;
        }
    }
}
