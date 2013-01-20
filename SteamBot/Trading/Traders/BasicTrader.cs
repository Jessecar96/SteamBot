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

        protected Dictionary<int, Inventory> myInventory;
        protected Dictionary<int, Inventory> theirInventory;
        protected Dictionary<int, Schema> schema;

        public void Start()
        {
            schema = new Dictionary<int, Schema>();
            myInventory = new Dictionary<int, Inventory>();
            theirInventory = new Dictionary<int, Inventory>();
            foreach(int appId in trade.bot.botConfig.AppIds)
            {
                schema.Add(appId, new Schema(appId, trade.bot.botConfig.ApiKey));
                myInventory.Add(appId, new Inventory(trade.botSid, appId, trade.bot.botConfig.ApiKey));
                theirInventory.Add(appId, new Inventory(trade.otherSid, appId, trade.bot.botConfig.ApiKey));
            }
        }

        public void OnStatusUpdate(Api.Status status)
        {

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

                            case Api.ETradeAction.UserAccept:
                                OnUserAccept(status, tradeEvent);
                                break;

                        }
                    }

                    numEvents = status.Events.Length;
                }
            }

            if (status.Success == false)
            {
                trade.DoLog(ELogType.WARN, "status.Success was false!");
            }
        }

        /// <summary>
        /// A convience method for bot.handler.HandleTradeClosed.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="reason">The reason for closing.</param>
        public virtual void CloseTrade(Api.ETradeStatus status)
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
            Inventory.Item item = theirInventory[tradeEvent.AppId].GetItem(tradeEvent.AssetId);
            Schema.Item schemaItem = item.GetSchemaItem(schema[tradeEvent.AppId]);
            trade.api.SendMessage(String.Format("You added {0}!", schemaItem.Name));
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
            trade.api.SetReady(true);
        }

        /// <summary>
        /// When the user unreadies.
        /// </summary>
        /// <param name="status">The status of the trade.</param>
        /// <param name="tradeEvent">The event itself.</param>
        public virtual void OnUserUnReady(Api.Status status, Api.TradeEvent tradeEvent)
        {
            trade.api.SendMessage("You unreadied!");
            trade.api.SetReady(false);
        }

        public virtual void OnUserAccept(Api.Status status, Api.TradeEvent tradeEvent)
        {
            trade.api.SendMessage("You accepted!");
            trade.api.AcceptTrade();
            trade.CloseTrade();
        }

    }
}
