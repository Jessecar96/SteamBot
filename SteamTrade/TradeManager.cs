using System;
using System.Threading;
using SteamKit2;
using SteamTrade.Exceptions;

namespace SteamTrade
{
    public class TradeManager
    {
        const int MaxGapTimeDefault = 30;
        const int MaxTradeTimeDefault = 180;
        const int TradePollingIntervalDefault = 800;
        string apiKey;
        string sessionId;
        string token;

        public TradeManager (string apiKey, string sessionId, string token)
        {
            if (apiKey == null)
                throw new ArgumentNullException ("apiKey");

            if (sessionId == null)
                throw new ArgumentNullException ("sessionId");

            if (token == null)
                throw new ArgumentNullException ("token");

            SetTradeTimeLimits (MaxTradeTimeDefault, MaxGapTimeDefault, TradePollingIntervalDefault);

            this.apiKey = apiKey;
            this.sessionId = sessionId;
            this.token = token;
        }

        #region Public Properties

        public int MaxTradeTimeSec
        {
            get; 
            private set;
        }

        public int MaxActionGapSec
        {
            get;
            private set;
        }

        public Inventory MyInventory
        {
            get; private set;
        }

        public Inventory OtherInventory
        {
            get; private set;
        }

        #endregion Public Properties

        /// <summary>
        /// Sets the trade time limits.
        /// </summary>
        /// <param name='maxTradeTime'>
        /// Max trade time in seconds.
        /// </param>
        /// <param name='maxActionGap'>
        /// Max gap between user action in seconds.
        /// </param>
        /// <param name='pollingInterval'>The trade polling interval in milliseconds.</param>
        public void SetTradeTimeLimits (int maxTradeTime, int maxActionGap, int pollingInterval)
        {
            Trade.MaximumTradeTime = maxTradeTime;
            Trade.MaximumActionGap = maxActionGap;
            Trade.TradePollingInterval = pollingInterval;
        }

        /// <summary>
        /// Creates a trade object, starts the trade, and returns it for use. 
        /// Call <see cref="FetchInventories"/> before using this method.
        /// </summary>
        /// <returns>
        /// The trade object to use to interact with the Steam trade.
        /// </returns>
        /// <param name='me'>
        /// The <see cref="SteamID"/> of the bot.
        /// </param>
        /// <param name='other'>
        /// The <see cref="SteamID"/> of the other trade partner.
        /// </param>
        /// <remarks>
        /// If the needed inventories are <c>null</c> then they will be fetched.
        /// </remarks>
        public Trade StartTrade (SteamID  me, SteamID other)
        {
            if (OtherInventory == null || MyInventory == null)
                InitializeTrade(me, other);

            var t = new Trade (me, other, sessionId, token, apiKey, MyInventory, OtherInventory);

            t.StartTrade ();

            return t;
        }

        /// <summary>
        /// Fetchs the inventories of both the bot and the other user as well as the TF2 item schema.
        /// </summary>
        /// <param name='me'>
        /// The <see cref="SteamID"/> of the bot.
        /// </param>
        /// <param name='other'>
        /// The <see cref="SteamID"/> of the other trade partner.
        /// </param>
        /// <remarks>
        /// This should be done anytime a new user is traded with or the inventories are out of date. It should
        /// be done sometime before calling <see cref="StartTrade"/>.
        /// </remarks>
        public void InitializeTrade (SteamID me, SteamID other)
        {
            // fetch other player's inventory from the Steam API.
            OtherInventory = Inventory.FetchInventory (other.ConvertToUInt64 (), apiKey);

            if (OtherInventory == null)
            {
                throw new InventoryFetchException (other);
            }
            
            // fetch our inventory from the Steam API.
            MyInventory = Inventory.FetchInventory (me.ConvertToUInt64 (), apiKey);

            if (MyInventory == null)
            {
                throw new InventoryFetchException (me);
            }
            
            // check that the schema was already successfully fetched
            if (Trade.CurrentSchema == null)
                Trade.CurrentSchema = Schema.FetchSchema (apiKey);

            if (Trade.CurrentSchema == null)
                throw new TradeException ("Could not download the latest item schema.");
        }
    }
}

