using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamBot
{
    /// <summary>
    /// A class that manages SteamBot processes.
    /// </summary>
    public class BotManager
    {
        private const string BotExecutable = "SteamBot.exe";
        private readonly List<RunningBotProc> botProcs;
        public Configuration ConfigObject { get; private set; }
        private Log mainLog;

        public BotManager()
        {
            new List<Bot>();
            botProcs = new List<RunningBotProc>();
        }

        /// <summary>
        /// Loads a configuration file to use when creating bots.
        /// </summary>
        /// <param name="configFile"><c>false</c> if there was problems loading the config file.</param>
        public bool LoadConfiguration(string configFile)
        {
            if (!File.Exists(configFile))
                return false;

            try
            {
                ConfigObject = Configuration.LoadConfiguration(configFile);
            }
            catch (JsonReaderException)
            {
                // handle basic json formatting screwups
                ConfigObject = null;
            }

            if (ConfigObject == null)
                return false;

            mainLog = new Log(ConfigObject.MainLog, null, Log.LogLevel.Debug);

            return true;
        }

        /// <summary>
        /// Starts the bots that have been configured.
        /// </summary>
        /// <returns><c>false</c> if there was something wrong with the configuration or logging.</returns>
        public bool StartBots()
        {
            if (ConfigObject == null || mainLog == null)
                return false;

            for (int i = 0; i < ConfigObject.Bots.Length; i++)
            {
                Configuration.BotInfo info = ConfigObject.Bots[i];
                mainLog.Info("Launching Bot " + info.DisplayName + "...");

                try
                {
                    SpawnSteamBotProcess(i, info);
                }
                catch (Exception e)
                {
                    mainLog.Error("Exception spawining SteamBot process: " + e);
                }

                Thread.Sleep(5000);
            }

            return true;
        }

        /// <summary>
        /// Kills all running bot processes.
        /// </summary>
        public void StopBots()
        {
            mainLog.Debug("Shutting down all bot processes.");
            foreach (var botProc in botProcs)
            {
                if (!botProc.BotProcess.HasExited)
                    botProc.BotProcess.Kill();
            }
        }

        /// <summary>
        /// Kills a single bot process given that bots index in the configuration.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        public void StopBot(int index)
        {
            mainLog.Debug(String.Format("Killing bot process {0}.", index));
            if (index < botProcs.Count)
            {
                if (!botProcs[index].BotProcess.HasExited)
                {
                    botProcs[index].BotProcess.Kill();
                    botProcs.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Stops a bot given that bots configured username.
        /// </summary>
        /// <param name="botUserName">The bot's username.</param>
        public void StopBot(string botUserName)
        {
            mainLog.Debug(String.Format("Killing bot process with username {0}.", botUserName));

            var res = from b in botProcs
                      where b.BotConfig.Username == botUserName
                      select b;

            foreach (var bot in res)
            {
                if (!bot.BotProcess.HasExited)
                {
                    bot.BotProcess.Kill();
                    botProcs.Remove(bot);
                }
            }
        }

        /// <summary>
        /// Starts a bot in a new process given that bot's index in the configuration.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        public void StartBot(int index)
        {
            mainLog.Debug(String.Format("Starting bot process {0}.", index));

            if (index < ConfigObject.Bots.Length)
            {
                SpawnSteamBotProcess(index, ConfigObject.Bots[index]);
            }
        }

        /// <summary>
        /// A method to return an instance of the <c>bot.BotControlClass</c>.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="sid">The steamId.</param>
        /// <returns>A <see cref="UserHandler"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the control class type does not exist.</exception>
        public static UserHandler UserHandlerCreator(Bot bot, SteamID sid)
        {
            Type controlClass = Type.GetType(bot.BotControlClass);

            if (controlClass == null)
                throw new ArgumentException("Configured control class type was null. You probably named it wrong in your configuration file.", "bot");

            return (UserHandler)Activator.CreateInstance(
                    controlClass, new object[] { bot, sid });
        }

        private void SpawnSteamBotProcess(int botIndex, Configuration.BotInfo botConfig)
        {
            // we don't do any of the standard output redirection below. 
            // we could but we lose the nice console colors that the Log class uses.

            Process botProc = new Process();
            botProc.StartInfo.FileName = BotExecutable;
            botProc.StartInfo.Arguments = @"-bot " + botIndex;

            // Set UseShellExecute to false for redirection.
            botProc.StartInfo.UseShellExecute = true;

            // Redirect the standard output.  
            // This stream is read asynchronously using an event handler.
            botProc.StartInfo.RedirectStandardOutput = false;

            // Set our event handler to asynchronously read the output.
            //botProc.OutputDataReceived += new DataReceivedEventHandler(BotStdOutHandler);

            botProc.Start();

            botProcs.Add(new RunningBotProc { BotConfig = botConfig, BotConfigIndex = botIndex, BotProcess = botProc });

            // Start the asynchronous read of the bot output stream.
            //botProc.BeginOutputReadLine();
        }

        private static void BotStdOutHandler(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        }

        /// <summary>
        /// Nested class that holds the information about a spawned bot process.
        /// </summary>
        private class RunningBotProc
        {
            public int BotConfigIndex { get; set; }
            public Configuration.BotInfo BotConfig { get; set; }
            public Process BotProcess { get; set; }
        }
    }
}
