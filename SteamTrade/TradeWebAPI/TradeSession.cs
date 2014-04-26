using System;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade.TradeWebAPI
{
    /// <summary>
    /// This class provides the interface into the Web API for trading on the
    /// Steam network.
    /// </summary>
    public class TradeSession
    {
        private const string SteamCommunityDomain = "steamcommunity.com";
        private const string SteamTradeUrl = "http://steamcommunity.com/trade/{0}/";

        private string sessionIdEsc;
        private string baseTradeURL;
        private CookieContainer cookies;

        private readonly string steamLogin;
        private readonly string sessionId;
        private readonly SteamID OtherSID;

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeSession"/> class.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="steamLogin">The current steam login.</param>
        /// <param name="otherSid">The Steam id of the other trading partner.</param>
        public TradeSession(string sessionId, string steamLogin, SteamID otherSid)
        {
            this.sessionId = sessionId;
            this.steamLogin = steamLogin;
            OtherSID = otherSid;

            Init();
        }

        #region Trade status properties
        
        /// <summary>
        /// Gets the LogPos number of the current trade.
        /// </summary>
        /// <remarks>This is not automatically updated by this class.</remarks>
        internal int LogPos { get; set; }

        /// <summary>
        /// Gets the version number of the current trade. This increments on
        /// every item added or removed from a trade.
        /// </summary>
        /// <remarks>This is not automatically updated by this class.</remarks>
        internal int Version { get; set; }

        #endregion Trade status properties

        #region Trade Web API command methods

        /// <summary>
        /// Gets the trade status.
        /// </summary>
        /// <returns>A deserialized JSON object into <see cref="TradeStatus"/></returns>
        /// <remarks>
        /// This is the main polling method for trading and must be done at a 
        /// periodic rate (probably around 1 second).
        /// </remarks>
        internal TradeStatus GetStatus()
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("logpos", "" + LogPos);
            data.Add ("version", "" + Version);
            
            string response = Fetch (baseTradeURL + "tradestatus", "POST", data);

            return JsonConvert.DeserializeObject<TradeStatus> (response);
        }

        /// <summary>
        /// Sends a message to the user over the trade chat.
        /// </summary>
        internal bool SendMessageWebCmd(string msg)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", sessionIdEsc);
            data.Add ("message", msg);
            data.Add ("logpos", "" + LogPos);
            data.Add ("version", "" + Version);

            string result = Fetch (baseTradeURL + "chat", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);
            return IsSuccess(json);
        }
        
        /// <summary>
        /// Adds a specified itom by its itemid.  Since each itemid is
        /// unique to each item, you'd first have to find the item, or
        /// use AddItemByDefindex instead.
        /// </summary>
        /// <returns>
        /// Returns false if the item doesn't exist in the Bot's inventory,
        /// and returns true if it appears the item was added.
        /// </returns>
        internal bool AddItemWebCmd(ulong itemid, int slot, int appid, long contextid, int amount)
        {
            var data = new NameValueCollection ();

            data.Add("sessionid", sessionIdEsc);
            data.Add("appid", "" + appid);
            data.Add("contextid", "" + contextid);
            data.Add("itemid", "" + itemid);
            data.Add("slot", "" + slot);
            data.Add("amount", "" + amount);

            string result = Fetch(baseTradeURL + "additem", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);
            return IsSuccess(json);
        }

        internal bool AddCurrencyWebCmd(ulong currencyid, int amount, int appid, long contextid)
        {
            var data = new NameValueCollection();

            data.Add("sessionid", sessionIdEsc);
            data.Add("appid", "" + appid);
            data.Add("contextid", "" + contextid);
            data.Add("currencyid", "" + currencyid);
            data.Add("amount", "" + amount);

            string result = Fetch(baseTradeURL + "setcurrency", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);
            return IsSuccess(json);
        }
        
        /// <summary>
        /// Removes an item by its itemid.  Read AddItem about itemids.
        /// Returns false if the item isn't in the offered items, or
        /// true if it appears it succeeded.
        /// </summary>
        internal bool RemoveItemWebCmd(ulong itemid, int slot, int appid, long contextid, int amount)
        {
            var data = new NameValueCollection ();

            data.Add("sessionid", sessionIdEsc);
            data.Add("appid", "" + appid);
            data.Add("contextid", "" + contextid);
            data.Add("itemid", "" + itemid);
            data.Add("slot", "" + slot);
            data.Add("amount", "" + amount);

            string result = Fetch (baseTradeURL + "removeitem", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);
            return IsSuccess(json);
        }
        
        /// <summary>
        /// Sets the bot to a ready status.
        /// </summary>
        internal bool SetReadyWebCmd(bool ready)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", sessionIdEsc);
            data.Add ("ready", ready ? "true" : "false");
            data.Add ("version", "" + Version);
            
            string result = Fetch (baseTradeURL + "toggleready", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);
            return IsSuccess(json);
        }
        
        /// <summary>
        /// Accepts the trade from the user.  Returns a deserialized
        /// JSON object.
        /// </summary>
        internal bool AcceptTradeWebCmd()
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("version", "" + Version);

            string response = Fetch (baseTradeURL + "confirm", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(response);
            return IsSuccess(json);
        }
        
        /// <summary>
        /// Cancel the trade.  This calls the OnClose handler, as well.
        /// </summary>
        internal bool CancelTradeWebCmd ()
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);

            string result = Fetch (baseTradeURL + "cancel", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);
            return IsSuccess(json);
        }

        private bool IsSuccess(dynamic json)
        {
            if(json == null)
                return false;
            try
            {
                //Sometimes, the response looks like this:  {"success":false,"results":{"success":11}}
                //I believe this is Steam's way of asking the trade window (which is actually a webpage) to refresh, following a large successful update
                return (json.success == "true" || (json.results != null && json.results.success == "11"));
            }
            catch(Exception)
            {
                return false;
            }
        }

        #endregion Trade Web API command methods
        
        string Fetch (string url, string method, NameValueCollection data = null)
        {
            return SteamWeb.Fetch (url, method, data, cookies);
        }

        private void Init()
        {
            sessionIdEsc = Uri.UnescapeDataString(sessionId);

            Version = 1;

            cookies = new CookieContainer();
            cookies.Add (new Cookie ("sessionid", sessionId, String.Empty, SteamCommunityDomain));
            cookies.Add (new Cookie ("steamLogin", steamLogin, String.Empty, SteamCommunityDomain));

            baseTradeURL = String.Format (SteamTradeUrl, OtherSID.ConvertToUInt64());
        }
    }
}

