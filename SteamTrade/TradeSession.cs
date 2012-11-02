using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade
{
    /// <summary>
    /// This class handles the web-based interaction for Steam trades.
    /// </summary>
    public class TradeSession
    {
        private static string SteamCommunityDomain = "steamcommunity.com";
        private static string SteamTradeUrl = "http://steamcommunity.com/trade/{0}/";

        private string sessionId;
        private SteamID otherTradingPartner;
        private string sessionIdEsc;
        private string baseTradeURL;
        private string steamLogin;
        private int version = 1;
        private CookieContainer cookies;
            
        public TradeSession (string sessionId, string token, SteamID otherTrader)
        {
            this.sessionId = sessionId;
            this.steamLogin = token;
            this.otherTradingPartner = otherTrader;

            this.sessionIdEsc = Uri.UnescapeDataString(sessionId);

            cookies = new CookieContainer();
            cookies.Add (new Cookie ("sessionid", sessionId, String.Empty, SteamCommunityDomain));
            cookies.Add (new Cookie ("steamLogin", steamLogin, String.Empty, SteamCommunityDomain));

            baseTradeURL = String.Format (SteamTradeUrl, otherTradingPartner.ConvertToUInt64 ());
        }

        internal int LogPos { get; set; }

        public StatusObj GetStatus ()
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("logpos", "" + LogPos);
            data.Add ("version", "" + version);
            
            string response = Fetch (baseTradeURL + "tradestatus", "POST", data);
            return JsonConvert.DeserializeObject<StatusObj> (response);
        }

        #region Trade interaction

        /// <summary>
        /// Sends a message to the user over the trade chat.
        /// </summary>
        public string SendMessage (string msg)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("message", msg);
            data.Add ("logpos", "" + LogPos);
            data.Add ("version", "" + version);
            return Fetch (baseTradeURL + "chat", "POST", data);
        }
        
        /// <summary>
        /// Adds a specified itom by its itemid.  Since each itemid is
        /// unique to each item, you'd first have to find the item, or
        /// use AddItemByDefindex instead.  It stores the item it added
        /// in MyOfferedItems.
        /// </summary>
        /// <returns>
        /// Returns false if the item doesn't exist in the Bot's inventory,
        /// and returns true if it appears the item was added.
        /// </returns>
        public bool AddItem (ulong itemid, int slot)
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);

            Fetch (baseTradeURL + "additem", "POST", data);
            return true;
        }
        
        /// <summary>
        /// Removes an item by its itemid.  Read AddItem about itemids.
        /// Returns false if the item isn't in the offered items, or
        /// true if it appears it succeeded.  Removes the item from
        /// MyOfferedItems.
        /// </summary>
        public bool RemoveItem (ulong itemid, int slot)
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);

            Fetch (baseTradeURL + "removeitem", "POST", data);

            return true;
        }
        
        /// <summary>
        /// Sets the bot to a ready status.
        /// </summary>
        public void SetReady (bool ready)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("ready", ready ? "true" : "false");
            data.Add ("version", "" + version);
            Fetch (baseTradeURL + "toggleready", "POST", data);
        }
        
        /// <summary>
        /// Accepts the trade from the user.  Returns a deserialized
        /// JSON object.
        /// </summary>
        public dynamic AcceptTrade ()
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("version", "" + version);
            string response = Fetch (baseTradeURL + "confirm", "POST", data);
            
            return JsonConvert.DeserializeObject (response);
        }
        
        /// <summary>
        /// Cancel the trade.  This calls the OnClose handler, as well.
        /// </summary>
        public void CancelTrade ()
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            Fetch (baseTradeURL + "cancel", "POST", data);
        }

        #endregion // Trade Interaction
        
        public string Fetch (string url, string method, NameValueCollection data = null)
        {
            return SteamWeb.Fetch (url, method, data, cookies);
        }

        public class StatusObj
        {
            public string error { get; set; }
            
            public bool newversion { get; set; }
            
            public bool success { get; set; }
            
            public long trade_status { get; set; }
            
            public int version { get; set; }
            
            public int logpos { get; set; }
            
            public TradeUserObj me { get; set; }
            
            public TradeUserObj them { get; set; }
            
            public TradeEvent[] events { get; set; }
        }

        public class TradeEvent
        {
            public string steamid { get; set; }
            
            public int action { get; set; }
            
            public ulong timestamp { get; set; }
            
            public int appid { get; set; }
            
            public string text { get; set; }
            
            public int contextid { get; set; }
            
            public ulong assetid { get; set; }
        }
        
        public class TradeUserObj
        {
            public int ready { get; set; }
            
            public int confirmed { get; set; }
            
            public int sec_since_touch { get; set; }
        }
    }


}

