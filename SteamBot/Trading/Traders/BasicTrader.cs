using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SteamBot.Trading.Traders
{
    class BasicTrader : ITrader
    {

        public Trade trade { get; set; }
        private int numEvents;

        public void OnStatusUpdate(Api.Status status)
        {
            trade.DoLog(ELogType.DEBUG, 
                String.Format("STATUS:\nerror: {0}, new_version: {1}, sucess: {2}, trade_status: {3}, version: {4}, logpos: {5}, events: {6}",
                status.Error, status.NewVersion, status.Success, status.TradeStatus, status.Version, status.Logpos, status.Events));

            if (status.NewVersion)
            {
                // this is important; steam will yell us if we don't do this
                trade.api.version = status.Version;

            }

            // we need to handle this stuff with dignity
                
            // let's check out the trade status first
            if (status.TradeStatus == Api.ETradeStatus.TradeCancelled ||
                status.TradeStatus == Api.ETradeStatus.TradeCompleted ||
                status.TradeStatus == Api.ETradeStatus.TradeFailed ||
                status.TradeStatus == Api.ETradeStatus.TradeTimedout)
            {
                CloseTrade(status.TradeStatus);
            }
            else
            {
                // now that we've determined that the trade is still open,
                // let's check the events and handle them.
                // the bot generates events, too, so we should check for those.
                if (status.Events != null && status.Events.Length > 0 && numEvents < status.Events.Length)
                {
                    trade.DoLog(ELogType.DEBUG, "NEW EVENTS");
                    for(int i = numEvents; i < status.Events.Length; i++)
                    {
                        Api.TradeEvent tradeEvent = status.Events[i];

                        switch (tradeEvent.Action)
                        {
                            case Api.ETradeAction.ItemAdd:
                                OnItemAdd(status, tradeEvent);
                                break;

                            case Api.ETradeAction.ItemRemove:
                                OnItemRemove(status, tradeEvent);
                                break;

                            case Api.ETradeAction.UserMessage:
                                OnUserMessage(status, tradeEvent);
                                break;

                            case Api.ETradeAction.UserReady:
                                OnUserReady(status, tradeEvent);
                                break;

                            case Api.ETradeAction.UserUnready:
                                OnUserUnReady(status, tradeEvent);
                                break;
                        }
                    }

                    numEvents = status.Events.Length;
                }
            }

            if (status.Success == false)
                trade.trading = false;
        }

        /// <summary>
        /// A convience method for bot.handler.HandleTradeClosed.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="reason">The reason for closing.</param>
        public void CloseTrade(Api.ETradeStatus status)
        {
            trade.CloseTrade();
            trade.bot.handler.HandleTradeClose(status);
        }

        /// <summary>
        /// When an item is added to the trade.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="tradeEvent">The event itself.</param>
        public virtual void OnItemAdd(Api.Status status, Api.TradeEvent tradeEvent)
        {
            trade.api.SendMessage("You added an item!");
        }

        /// <summary>
        /// When an item is removed from the trade.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="tradeEvent">The event itself.</param>
        public virtual void OnItemRemove(Api.Status status, Api.TradeEvent tradeEvent)
        {
            trade.api.SendMessage("You removed an item!");
        }

        /// <summary>
        /// When a user messages in the trade.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="tradeEvent">The event itself.</param>
        public virtual void OnUserMessage(Api.Status status, Api.TradeEvent tradeEvent)
        {
            if (tradeEvent.SteamId == trade.otherSid)
                trade.api.SendMessage("You messaged me!");
        }

        /// <summary>
        /// When the user readies up.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="tradeEvent">The event itself.</param>
        public virtual void OnUserReady(Api.Status status, Api.TradeEvent tradeEvent)
        {
            trade.api.SendMessage("You readied!");
        }

        /// <summary>
        /// When the user unreadies.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="tradeEvent">The event itself.</param>
        public virtual void OnUserUnReady(Api.Status status, Api.TradeEvent tradeEvent)
        {
            trade.api.SendMessage("You unreadied!");
        }

    }
}
