﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamBot
{
    /// <summary>
    /// A class that manages SteamBot processes.
    /// </summary>
    public class BotManager
    {
        private readonly List<RunningBot> botProcs;
        private Log mainLog;
        private bool useSeparateProcesses;

        public BotManager()
        {
            useSeparateProcesses = false;
            new List<Bot>();
            botProcs = new List<RunningBot>();
        }

        public Configuration ConfigObject { get; private set; }

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

            useSeparateProcesses = ConfigObject.UseSeparateProcesses;

            mainLog = new Log(ConfigObject.MainLog, null, Log.LogLevel.Debug);

            for (int i = 0; i < ConfigObject.Bots.Length; i++)
            {
                Configuration.BotInfo info = ConfigObject.Bots[i];
                mainLog.Info("Launching Bot " + info.DisplayName + "...");

                var v = new RunningBot(useSeparateProcesses, i, ConfigObject);
                botProcs.Add(v);
            }

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

            foreach (var runningBot in botProcs)
            {
                runningBot.Start();

                Thread.Sleep(2000);
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
                botProc.Stop();
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
                botProcs[index].Stop();
            }
        }

        /// <summary>
        /// Stops a bot given that bots configured username.
        /// </summary>
        /// <param name="botUserName">The bot's username.</param>
        public void StopBot(string botUserName)
        {
            mainLog.Debug(String.Format("Killing bot with username {0}.", botUserName));

            var res = from b in botProcs
                      where b.BotConfig.Username == botUserName
                      select b;

            foreach (var bot in res)
            {
                bot.Stop();
            }
        }

        /// <summary>
        /// Starts a bot in a new process given that bot's index in the configuration.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        public void StartBot(int index)
        {
            mainLog.Debug(String.Format("Starting bot at index {0}.", index));

            if (index < ConfigObject.Bots.Length)
            {
                botProcs[index].Start();
            }
        }

        /// <summary>
        /// Starts a bot given that bots configured username.
        /// </summary>
        /// <param name="botUserName">The bot's username.</param>
        public void StartBot(string botUserName)
        {
            mainLog.Debug(String.Format("Starting bot with username {0}.", botUserName));

            var res = from b in botProcs
                      where b.BotConfig.Username == botUserName
                      select b;

            foreach (var bot in res)
            {
                bot.Start();
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

        #region Nested RunningBot class

        /// <summary>
        /// Nested class that holds the information about a spawned bot process.
        /// </summary>
        private class RunningBot
        {
            private const string BotExecutable = "SteamBot.exe";
            private readonly Configuration config;

            /// <summary>
            /// Creates a new instance of <see cref="RunningBot"/> class.
            /// </summary>
            /// <param name="useProcesses">
            /// <c>true</c> indicates that this bot is ran in a thread;
            /// <c>false</c> indicates it is ran in a separate process.
            /// </param>
            /// <param name="index">The index of the bot in the configuration.</param>
            /// <param name="config">The bots configuration object.</param>
            public RunningBot(bool useProcesses, int index, Configuration config)
            {
                this.config = config;
                UsingProcesses = useProcesses;
                BotConfigIndex = index;
                BotConfig = config.Bots[BotConfigIndex];
                this.config = config;
            }

            public bool UsingProcesses { get; private set; }

            public int BotConfigIndex { get; private set; }

            public Configuration.BotInfo BotConfig { get; private set; }

            public Process BotProcess { get; set; }

            // will not be null in threaded mode. will be null in process mode.
            public Bot TheBot { get; set; }

            public void Stop()
            {
                if (UsingProcesses)
                {
                    if (!BotProcess.HasExited)
                        BotProcess.Kill();
                }
                else
                {
                    if (TheBot != null)
                        TheBot.StopBot();
                }
            }

            public void Start()
            {
                if (UsingProcesses)
                {
                    SpawnSteamBotProcess(BotConfigIndex);
                }
                else
                {
                    SpawnBotThread(BotConfig);
                }  
            }

            private void SpawnSteamBotProcess(int botIndex)
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

                BotProcess = botProc;

                // Start the asynchronous read of the bot output stream.
                //botProc.BeginOutputReadLine();
            }


            private void SpawnBotThread(Configuration.BotInfo botConfig)
            {
                // the bot object itself is threaded so we just build it and start it.
                Bot b = new Bot(botConfig,
                                config.ApiKey,
                                UserHandlerCreator,
                                true);

                b.OnSteamGuardRequired += BotOnOnSteamGuardRequired;

                TheBot = b;
                TheBot.StartBot();
            }

            private void BotOnOnSteamGuardRequired(object sender, SteamGuardRequiredEventArgs barf)
            {
                var bot = sender as Bot;
                var window = new SteamGuardForm(bot.DisplayName);
                var dialogResult = window.ShowDialog();

                if (dialogResult == DialogResult.OK)
                {
                    barf.SteamGuard = window.UserEnteredCode;
                }
            }

            //private static void BotStdOutHandler(object sender, DataReceivedEventArgs e)
            //{
            //    if (!String.IsNullOrEmpty(e.Data))
            //    {
            //        Console.WriteLine(e.Data);
            //    }
            //}
        }

        #endregion Nested RunningBot class
    }
}
