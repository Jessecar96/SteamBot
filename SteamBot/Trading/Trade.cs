using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SteamKit2;

namespace SteamBot.Trading
{
    public class Trade
    {
        public Api api;
        public ITrader trader;
        public Bot bot;

        private Thread statusThread;
        private Thread pollThread;
        private Mutex apiMutex;

        public bool trading = true;

        public Trade(SteamID otherSID, Bot bot)
        {
            Start(otherSID, bot, bot.botConfig.Trader);
        }

        public Trade(SteamID otherSID, Bot bot, Type trader)
        {
            Start(otherSID, bot, trader);
        }

        void Start(SteamID otherSid, Bot bot, Type trader)
        {
            this.bot = bot;
            this.trader = (ITrader)System.Activator.CreateInstance(trader);
            this.trader.trade = this;
            statusThread = null;

            api = new Api(otherSid, bot.handler);
            api.StatusUpdater = UpdateStatus;
            apiMutex = new Mutex();

            pollThread = new Thread(() =>
            {
                while (trading)
                {
                    //DoLog(ELogType.INFO, "(POLL) AWAITING LOCK ON API...");
                    apiMutex.WaitOne();
                    //DoLog(ELogType.INFO, "(POLL) GOT LOCK ON API.");
                    api.GetStatus();
                    //DoLog(ELogType.INFO, "(POLL) RELEASED LOCK ON API.");
                    apiMutex.ReleaseMutex();
                    Thread.Sleep(800);
                }
            });
            pollThread.Start();
        }

        public void UpdateStatus(Api.Status status)
        {
            if (statusThread == null || 
                statusThread.ThreadState == ThreadState.Stopped ||
                statusThread.ThreadState == ThreadState.Aborted)
            {
                statusThread = new Thread(() =>
                {
                    //DoLog(ELogType.INFO, "(STATUS) AWAITING LOCK ON API...");
                    apiMutex.WaitOne();
                    //DoLog(ELogType.INFO, "(STATUS) GOT LOCK ON API.");
                    trader.OnStatusUpdate(status);
                    apiMutex.ReleaseMutex();
                    //DoLog(ELogType.INFO, "(STATUS) RELEASED LOCK ON API.");
                });
                statusThread.Start();
            }
        }

        public void DoLog(ELogType type, string log)
        {
            bot.botConfig.runner.DoLog(type, bot.botConfig.BotName + " (trade)", log);
        }
    }
}
