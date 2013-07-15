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
        static string SteamCommunityDomain = "steamcommunity.com";
        static string SteamTradeUrl = "http://steamcommunity.com/trade/{0}/";

        string sessionIdEsc;
        string baseTradeURL;
        CookieContainer cookies;

        readonly string steamLogin;
        readonly string sessionId;
        readonly SteamID OtherSID;
        readonly string appIdValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeSession"/> class.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="steamLogin">The current steam login.</param>
        /// <param name="otherSid">The Steam id of the other trading partner.</param>
        /// <param name="appId">The Steam app id. Ex. "440" for TF2</param>
        public TradeSession(string sessionId, string steamLogin, SteamID otherSid, string appId)
        {
            this.sessionId = sessionId;
            this.steamLogin = steamLogin;
            OtherSID = otherSid;
            appIdValue = appId;

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
        /// Gets the foriegn inventory.
        /// </summary>
        /// <param name="otherId">The other id.</param>
        /// <param name="contextId">The current trade context id.</param>
        /// <returns>A dynamic JSON object.</returns>
        internal dynamic GetForiegnInventory(SteamID otherId, int contextId)
        {
            var data = new NameValueCollection();

            data.Add("sessionid", sessionIdEsc);
            data.Add("steamid", otherId.ConvertToUInt64().ToString());
            data.Add("appid", appIdValue);
            data.Add("contextid", contextId.ToString());

            try
            {
                string response = Fetch(baseTradeURL + "foreigninventory", "POST", data);
                return JsonConvert.DeserializeObject(response);
            }
            catch (Exception)
            {
                return JsonConvert.DeserializeObject("{\"success\":\"false\"}");
            }
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

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
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
        internal bool AddItemWebCmd(ulong itemid, int slot)
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("appid", appIdValue);
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);

            string result = Fetch(baseTradeURL + "additem", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Removes an item by its itemid.  Read AddItem about itemids.
        /// Returns false if the item isn't in the offered items, or
        /// true if it appears it succeeded.
        /// </summary>
        internal bool RemoveItemWebCmd(ulong itemid, int slot)
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("appid", appIdValue);
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);

            string result = Fetch (baseTradeURL + "removeitem", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
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

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
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

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
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

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
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

