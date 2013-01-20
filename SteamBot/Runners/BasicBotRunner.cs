using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using SteamBot;
using SteamBot.Trading;
using Newtonsoft.Json;

namespace SteamBot.Runners
{
    /// <summary>
    /// Console bot runner.  Despite its name, it also logs to a file, as well.
    /// The log file(s) are taken from the arguments passed to the file.
    /// </summary>
    public class BasicBotRunner : IBotRunner
    {

        private ELogType LogLevel;

        protected StreamWriter fileStream;

        protected Dictionary<Thread, Bot> botList;
        //protected List<Bot> botList;
        //protected List<Thread> botThreads;

        public void Start (Options options) 
        {
            this.LogLevel = options.LogLevel;
            fileStream = File.AppendText (options.LogFile);
            fileStream.AutoFlush = true;

            string configFile = File.ReadAllText(options.ReadFile);
            Configuration config = JsonConvert.DeserializeObject<Configuration>(configFile);
            //botList = new List<Bot>();
            botList = new Dictionary<Thread, Bot>();
            //botThreads = new List<Thread>();

            if (!options.SkipSchema && config.AppIds != null && config.AppIds.Length > 0)
            {
                DoLog(ELogType.INFO, "Caching Schema...");

                foreach (int appId in config.AppIds)
                {
                    Schema schema = new Schema(appId, config.ApiKey);
                }
            }

            foreach (BotConfiguration botConf in config.Bots)
            {
                BotConfig botConfig = new BotConfig
                {
                    Username      = botConf.Username,
                    Password      = botConf.Password,
                    ApiKey        = config.ApiKey,
                    BotName       = botConf.DisplayName,
                    SentryFile    = botConf.SentryFile,
                    Authenticator = typeof(Trading.Authenticator.SteamUserAuth),

                    AppIds = botConf.AppIds == null ? config.AppIds : botConf.AppIds,
                    Trader = typeof(Trading.Traders.BasicTrader),
                    runner = this
                };

                Bot bot = new Bot(botConfig, typeof(Handlers.BasicBotHandler));
                Thread botThread = new Thread(() =>
                {
                    bot.Start();
                });
                //botList.Add(bot);
                //botThreads.Add(botThread);
                botList.Add(botThread, bot);
                botThread.Start();
            }

            Console.CancelKeyPress += HandleShutdown;

        }

        /// <summary>
        /// Causes the shutdown of all of the bots.  First goes through the
        /// bots calling Exit, then loops through the thread one by one
        /// calling Join on it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleShutdown(object sender, ConsoleCancelEventArgs e)
        {
            DoLog(ELogType.INTERFACE, "Shutting Down");
            foreach (Bot bot in botList.Values)
            {
                bot.Exit();
            }

            foreach (Thread botThread in botList.Keys)
            {
                botThread.Join();
            }
        }

        public void DoLog (ELogType type, string log)
        {
            DoLog (type, "(system)", log);
        }

        public void DoLog (ELogType type, string name, string log)
        {
            string formattedString = String.Format ("[{0} {1}] {2}: {3}", name,
                                DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss"),
                                type.ToString (), log);
            if (type >= this.LogLevel)
            {
                Console.WriteLine (formattedString);
            }

            fileStream.WriteLine (formattedString);
        }

        public string GetSteamGuardCode()
        {
            return Console.ReadLine();
        }

        class Configuration
        {
            /// <summary>
            /// The API Key for the Steam API.
            /// </summary>
            [JsonProperty("api_key",
                Required = Required.Always)]
            public string ApiKey { get; set; }

            /// <summary>
            /// The Bot array.  <see cref="BotConfiguration"/>.
            /// </summary>
            [JsonProperty("bots",
                Required = Required.Always)]
            public BotConfiguration[] Bots { get; set; }

            /// <summary>
            /// The App Ids to support.  Used for Schema caching.  Not
            /// required.
            /// </summary>
            [JsonProperty("app_ids")]
            public int[] AppIds { get; set; }
        }

        class BotConfiguration
        {
            /// <summary>
            /// The username the bot uses to log in with.
            /// </summary>
            [JsonProperty("username",
                Required = Required.Always)]
            public string Username { get; set; }

            /// <summary>
            /// The password the bot uses to log in with.
            /// </summary>
            [JsonProperty("password",
                Required = Required.Always)]
            public string Password { get; set; }

            /// <summary>
            /// The name the bot will use to display with.
            /// </summary>
            [JsonProperty("display_name",
                Required = Required.Always)]
            public string DisplayName { get; set; }

            /// <summary>
            /// The sentry file for bot authentication.
            /// </summary>
            [JsonProperty("sentry_file",
                Required = Required.Always)]
            public string SentryFile { get; set; }

            /// <summary>
            /// The app ids the bot should support.  If this doesn't exist, it
            /// will use the global app ids (<see cref="Configuration"/>).
            /// </summary>
            [JsonProperty("app_ids")]
            public int[] AppIds { get; set; }
        }
    }
}

