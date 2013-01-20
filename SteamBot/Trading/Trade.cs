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
        public SteamID botSid;
        public SteamID otherSid;

        private Thread statusThread;
        private Thread pollThread;
        private Mutex apiMutex;

        public volatile bool trading = true;

        public Trade(SteamID otherSID, Bot bot)
        {
            Start(otherSID, bot, bot.botConfig.Trader);
        }

        public Trade(SteamID otherSID, Bot bot, Type trader)
        {
            Start(otherSID, bot, trader);
        }

        public void CloseTrade()
        {
            this.trading = false;
            pollThread.Join(500);
            if (statusThread != null)
                statusThread.Join(500);
        }

        void Start(SteamID otherSid, Bot bot, Type traderType)
        {
            this.bot = bot;
            this.otherSid = otherSid;
            trader = (ITrader)System.Activator.CreateInstance(traderType);
            trader.trade = this;
            botSid = bot.steamId;
            trader.Start();
            statusThread = null;

            api = new Api(otherSid, bot.handler);
            api.StatusUpdater = UpdateStatus;
            apiMutex = new Mutex();

            pollThread = new Thread(() =>
            {
                while (trading)
                {
                    apiMutex.WaitOne();
                    api.GetStatus();
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
                    apiMutex.WaitOne();
                    trader.OnStatusUpdate(status);
                    apiMutex.ReleaseMutex();
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
