using System;
using System.Collections.Generic;
using System.Threading;
using SteamKit2;

namespace SteamBot
{
    public class BotManager
    {
        private readonly List<Thread> botThreads;
        private Configuration configObject;
        private Log mainLog;

        public BotManager()
        {
            botThreads = new List<Thread>();
        }

        /// <summary>
        /// Loads a configuration file to use when creating bots.
        /// </summary>
        /// <param name="configFile"><c>false</c> if there was problems loading the config file.</param>
        public bool LoadConfiguration(string configFile)
        {
            if (!System.IO.File.Exists(configFile))
                return false;

            try
            {
                configObject = Configuration.LoadConfiguration(configFile);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                // handle basic json formatting screwups
                configObject = null;
            }

            if (configObject == null)
                return false;

            mainLog = new Log(configObject.MainLog, null);

            return true;
        }

        /// <summary>
        /// Starts the bots that have been configured.
        /// </summary>
        /// <returns><c>false</c> if there was something wrong with the configuration or logging.</returns>
        public bool StartBots()
        {
            if (configObject == null || mainLog == null)
                return false;

            foreach (Configuration.BotInfo info in configObject.Bots)
            {
                mainLog.Info("Launching Bot " + info.DisplayName + "...");

                var thread = new Thread(BotThread);

                botThreads.Add(thread);

                thread.Start(info);

                Thread.Sleep(5000);
            }

            return true;
        }

        private void BotThread(object botInfo)
        {
            var info = botInfo as Configuration.BotInfo;

            int crashes = 0;
            while (crashes < 1000)
            {
                try
                {
                    //  we don't return from this.
                    new Bot(info, configObject.ApiKey, UserHandlerCreator, false);
                }
                catch (Exception e)
                {
                    mainLog.Error("Error With Bot: " + e);
                    crashes++;
                }
            }
        }

        public UserHandler UserHandlerCreator(Bot bot, SteamID sid)
        {
            Type controlClass = Type.GetType(bot.BotControlClass);

            if (controlClass == null)
                throw new ArgumentException("Configured control class type was null. You probably named it wrong in your configuration file.", "bot.BotControlClass");

            return (SteamBot.UserHandler)System.Activator.CreateInstance(
                    controlClass, new object[] { bot, sid });
        }
    }
}
