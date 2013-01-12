using System;
using System.IO;
using System.Threading;
using SteamBot;

namespace SteamBot.Runners
{
    /// <summary>
    /// Console bot runner.  Despite its name, it also logs to a file, as well.
    /// The log file(s) are taken from the arguments passed to the file.
    /// </summary>
    public class ConsoleBotRunner : IBotRunner
    {

        private ELogType LogLevel;

        protected StreamWriter fileStream;

        public void Start (Options options) 
        {
            this.LogLevel = options.LogLevel;
            fileStream = File.AppendText (options.LogFile);
            fileStream.AutoFlush = true;

            BotConfig botConfig = new BotConfig
            {
                Username = SteamBot.Default.BotUserName,
                Password = SteamBot.Default.BotPassword,
                ApiKey = SteamBot.Default.ApiKey,
                BotName = SteamBot.Default.Name,
                SentryFile = SteamBot.Default.SentryFile,
                Authenticator = typeof(Trading.Authenticator.SteamUserAuth),
                Trader = typeof(Trading.Traders.ConsoleTrader),
                runner = this
            };
            Bot bot = new Bot(botConfig, typeof(Handlers.ConsoleBotHandler));
            Thread botThread = new Thread( ()=>
            {
                bot.Start();
            });
            botThread.Start();
            string cLine;
            bool run = true;
            while (run)
            {
                cLine = Console.ReadLine();
                if (cLine == "disconnect")
                {
                    run = false;
                    bot.Exit();
                    botThread.Join();
                }
                else
                {
                    DoLog(ELogType.INFO, cLine);
                }
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
    }
}

