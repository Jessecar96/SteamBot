using System;
using System.Text;
using SteamKit2;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Web;

namespace SteamBot.Trading
{

    public class Api
    {

        public Web web { get; set; }
        public BotHandler botHandler { get; set; }
        public SteamID otherSID { get; set; }

        private string sessionId;
        private string steamLogin;
        private string baseTradeUri;

        /// <summary>
        /// The position of the event log we are on.
        /// </summary>
        public int logPos { get; set; }

        /// <summary>
        /// The version of the item lists each player has put up.
        /// </summary>
        public int version { get; set; }

        /// <summary>
        /// Becaause whenever we do an action, such as chat, additem, or
        /// removeitem, the server sends back a status.  This is always
        /// called whenever we recieve a status (even in GetStatus).
        /// </summary>
        /// <param name="status">The status the server sent.</param>
        /// <returns></returns>
        public delegate void StatusUpdate(Status status);
        public StatusUpdate StatusUpdater;

        static string SteamTradeUri = "/trade/{0}/";
        static string SteamCommunityDomain = "steamcommunity.com";

        public Api(SteamID OtherSID, BotHandler botHandler)
        {
            this.otherSID = OtherSID;
            this.botHandler = botHandler;
            this.web = new Web(botHandler.web);
            web.Domain = SteamCommunityDomain;
            web.Scheme = "http";
            web.ActAsAjax = true;
            logPos = 0;
            version = 1;

            sessionId = Uri.UnescapeDataString(botHandler.sessionId);
            steamLogin = botHandler.steamLogin;
            baseTradeUri = String.Format(SteamTradeUri, otherSID.ConvertToUInt64().ToString());
        }

        /// <summary>
        /// Retrieves the status of the server and runs it through StatusUpdater.
        /// </summary>
        /// <returns>The status of the server.</returns>
        public Status GetStatus()
        {
            string result = web.Do(baseTradeUri + "tradestatus", "POST", GetData());
            Status status = JsonConvert.DeserializeObject<Status>(result);
            StatusUpdater(status);
            return status;
        }

        /// <summary>
        /// Send a message to the chat.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public Status SendMessage(string message)
        {
            NameValueCollection data = GetData();

            data.Add("message", message);

            return HandleStatus(baseTradeUri + "chat", "POST", data);
        }

        /// <summary>
        /// Add an item to the trade.
        /// </summary>
        /// <param name="appId">The app Id of the game.</param>
        /// <param name="item">The item to add.</param>
        /// <param name="slot">The slot to put the item in.</param>
        public Status AddItem(int appId, Inventory.Item item, int slot)
        {
            NameValueCollection data = GetData();

            data.Add("appid", appId.ToString());
            data.Add("contextid", "2");
            data.Add("itemid", item.Id.ToString());
            data.Add("slot", slot.ToString());

            return HandleStatus(baseTradeUri + "additem", "POST", data);
        }

        /// <summary>
        /// Remove an item from the trade.
        /// </summary>
        /// <param name="appId">The app id of the game.</param>
        /// <param name="item">The inventory item.</param>
        /// <param name="slot">The slot to put it in.</param>
        public Status RemoveItem(int appId, Inventory.Item item, int slot)
        {
            NameValueCollection data = GetData();

            data.Add("appid", appId.ToString());
            data.Add("contextid", "2");
            data.Add("itemid", item.Id.ToString());
            data.Add("slot", slot.ToString());

            return HandleStatus(baseTradeUri + "removeitem", "POST", data);
        }

        /// <summary>
        /// Set the status of the bot to ready.
        /// </summary>
        /// <param name="ready">Whether or not to be ready.</param>
        public Status SetReady(bool ready)
        {
            NameValueCollection data = GetData();

            data.Add("ready", ready ? "true" : "false");

            return HandleStatus(baseTradeUri + "toggleready", "POST", data);
        }

        /// <summary>
        /// Accept this trade.
        /// </summary>
        public Status AcceptTrade()
        {
            NameValueCollection data = GetData();

            return HandleStatus(baseTradeUri + "confirm", "POST", data);
        }

        public Status CancelTrade()
        {
            NameValueCollection data = GetData();

            return HandleStatus(baseTradeUri + "cancel", "POST", data);
        }

        NameValueCollection GetData()
        {
            NameValueCollection data = new NameValueCollection();

            data.Add("sessionid", sessionId);
            data.Add("logpos", "" + logPos);
            data.Add("version", "" + version);

            return data;
        }

        Status HandleStatus(string uri, string method, NameValueCollection data)
        {
            string result = web.Do(uri, method, data);
            Status status = JsonConvert.DeserializeObject<Status>(result);
            StatusUpdater(status);
            return status;
        }

        #region JSON Responses
        public class Status
        {

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("newversion")]
            public bool NewVersion { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("trade_status")]
            public ETradeStatus TradeStatus { get; set; }

            [JsonProperty("version")]
            public int Version { get; set; }

            [JsonProperty("logpos")]
            public int Logpos { get; set; }

            [JsonProperty("me")]
            public TradeUser Bot { get; set; }

            [JsonProperty("them")]
            public TradeUser Other { get; set; }

            [JsonProperty("events")]
            public TradeEvent[] Events { get; set; }

        }

        public class TradeUser 
        {

            [JsonProperty("ready")]
            public int Ready { get; set; }

            [JsonProperty("confirmed")]
            public int Confirmed { get; set; }

            [JsonProperty("sec_since_touch")]
            public int SecondsSinceTouch { get; set; }

        }

        public class TradeEvent
        {

            [JsonProperty("steamid")]
            public ulong _SteamId
            {
                get
                {
                    return SteamId.ConvertToUInt64();
                }

                set
                {
                    SteamId = new SteamID(value);
                }
            }

            [JsonIgnore]
            public SteamID SteamId { get; set; }

            [JsonProperty("action")]
            public ETradeAction Action { get; set; }

            [JsonProperty("timestamp")]
            public ulong Timestamp { get; set; }

            [JsonProperty("appid")]
            public int AppId { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("contextid")]
            public int ContextId { get; set; }

            [JsonProperty("assetid")]
            public ulong AssetId { get; set; }

        }

        /*
         * Trade Status
         * 1 - Trade Completed
         * 2 - 
         * 3 - Trade Cancelled (by them)
         * 4 - Parter Timed out
         * 5 - Failed (?)
         */
        public enum ETradeStatus
        {
            TradeInProgress = 0,
            TradeCompleted  = 1,
            TradeCancelled  = 3,
            TradeTimedout   = 4,
            TradeFailed     = 5
        }

        /*
         * Event Actions
         * 0 - Add Item
         * 1 - Remove Item
         * 2 - Ready
         * 3 - Unready
         * 4 - Accept(?)
         * 5 - 
         * 6 - Currency(?)
         * 7 - Message
         */
        public enum ETradeAction
        {
            ItemAdd        = 0,
            ItemRemove     = 1,
            UserReady      = 2,
            UserUnready    = 3,
            UserAccept     = 4,
            CurrencyChange = 6,
            UserMessage    = 7
        }
        #endregion
    }
}
