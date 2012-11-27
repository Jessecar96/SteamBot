using System;
using System.Threading;

namespace SteamTrade
{
    public partial class Trade
    {
        public static int TradePollingInterval
        {
            get;
            internal set;
        }

        public bool IsTradeThreadRunning
        {
            get;
            internal set;
        }

        internal void StartTradeThread ()
        {
            new Thread (() =>
            {
                IsTradeThreadRunning = true;

                // main thread loop for polling
                while (IsTradeThreadRunning)
                {
                    Thread.Sleep (TradePollingInterval);

                    try
                    {
                        Poll ();
                            
                        if (OtherUserCancelled)
                        {
                            IsTradeThreadRunning = false;

                            if (OnClose != null)
                                OnClose ();
                        }
                    }
                    catch (Exception e)
                    {
                        if (OnError != null)
                            OnError ("Error Polling Trade: " + e);

                        // ok then we should stop polling...
                        IsTradeThreadRunning = false;
                    }
                }
            }).Start ();
        }
    }
}

