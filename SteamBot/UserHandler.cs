using System.Linq;
using SteamKit2;

namespace SteamBot
{
    public abstract class UserHandler
    {
        protected Bot Bot;
        protected SteamID OtherSID;
        protected Log Log {
            get { return Bot.log; }
        }
        protected bool IsAdmin {
            get { return Bot.Admins.Contains (OtherSID); }
        }
        protected Trade Trade;

        public UserHandler (Bot bot, SteamID sid)
        {
            Bot = bot;
            OtherSID = sid;
        }

        /// <summary>
        /// Called when a the user adds the bot as a friend.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public abstract bool OnFriendAdd();

        public abstract void OnFriendRemove();

        public abstract void OnMessage(string message, EChatEntryType type);

        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public abstract bool OnTradeRequest();

        #region Trade events
        /// <summary>
        /// Subscribes all listeners of this to the trade.
        /// </summary>
        public void SubscribeTrade (Trade trade)
        {
            trade.OnError += OnTradeError;
            trade.OnTimeout += OnTradeTimeout;
            trade.OnAfterInit += OnTradeInit;
            trade.OnUserAddItem += OnTradeAddItem;
            trade.OnUserRemoveItem += OnTradeRemoveItem;
            trade.OnMessage += OnTradeMessage;
            trade.OnUserSetReady += OnTradeReady;
            trade.OnUserAccept += OnTradeAccept;
            Trade = trade;
        }

        /// <summary>
        /// Unsubscribes all listeners of this from the current trade.
        /// </summary>
        public void UnsubscribeTrade ()
        {
            Trade.OnError -= OnTradeError;
            Trade.OnTimeout -= OnTradeTimeout;
            Trade.OnAfterInit -= OnTradeInit;
            Trade.OnUserAddItem -= OnTradeAddItem;
            Trade.OnUserRemoveItem -= OnTradeRemoveItem;
            Trade.OnMessage -= OnTradeMessage;
            Trade.OnUserSetReady -= OnTradeReady;
            Trade.OnUserAccept -= OnTradeAccept;
            Trade = null;
        }

        public abstract void OnTradeError (string error);

        public abstract void OnTradeTimeout ();

        public abstract void OnTradeInit();

        public abstract void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem);

        public abstract void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem);

        public abstract void OnTradeMessage(string message);

        public abstract void OnTradeReady(bool ready);

        public abstract void OnTradeAccept();
        #endregion

    }
}
