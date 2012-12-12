using System.Linq;
using SteamKit2;
using SteamTrade;

namespace SteamBot
{
    public abstract class UserHandler
    {
        protected Bot Bot;
        protected SteamID OtherSID;
        protected Log Log 
        {
            get { return Bot.log; }
        }
        protected bool IsAdmin 
        {
            get { return Bot.Admins.Contains (OtherSID); }
        }
        //protected Trade Trade;

        public UserHandler (Bot bot, SteamID sid)
        {
            Bot = bot;
            OtherSID = sid;
        }

        public Trade Trade 
        {
            get 
            {
                return Bot.CurrentTrade; 
            }
        }

        /// <summary>
        /// Called when a the user adds the bot as a friend.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public abstract bool OnFriendAdd();

        public abstract void OnFriendRemove();

        /// <summary>
        /// Called whenever a message is sent to the bot.
        /// This is limited to regular and emote messages.
        /// </summary>
        public abstract void OnMessage(string message, EChatEntryType type);

        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public abstract bool OnTradeRequest();

        #region Trade events

        public abstract void OnTradeError (string error);

        public abstract void OnTradeTimeout ();

        public virtual void OnTradeClose ()
        {
            Bot.log.Warn ("[USERHANDLER] TRADE CLOSED");
            Bot.CloseTrade ();
        }

        public abstract void OnTradeInit ();

        public abstract void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem);

        public abstract void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem);

        public abstract void OnTradeMessage (string message);

        public abstract void OnTradeReady (bool ready);

        public abstract void OnTradeAccept ();
        #endregion

    }
}
