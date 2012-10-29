using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamBot
{
    public class Trade
    {
        #region Static
        // Static properties
        public static string SteamCommunityDomain = "steamcommunity.com";
        public static string SteamTradeUrl = "http://steamcommunity.com/trade/{0}/";
        public static Schema CurrentSchema = null;
        #endregion

        #region Properties
        // The bot's steam ID.
        public SteamID MeSID;

        // The other user's steam ID.
        public SteamID OtherSID;

        // The bot itself.
        public Bot bot;

        // Generic Trade info

        // If the bot is ready.
        public bool MeReady = false;

        // If the other user is ready.
        public bool OtherReady = false;

        // Whether or not the trade actually started.
        bool tradeStarted = false;

        // When the trade started.
        public DateTime TradeStart;

        // When the last action taken by the user was.
        public DateTime LastAction;

        // The maximum trading time the bot will take.
        private int _MaxTradeTime;

        // The maximum amount of time the bot will wait
        // between actions.
        private int _MaxActionGap;

        // The maximum trading time the bot will take.  Will not
        // take any value lower than 15.
        public int MaximumTradeTime
        {
            get 
            {
                return _MaxTradeTime;
            }
            set
            {
                _MaxTradeTime = value <= 15 ? 15 : value;
            }
        }

        // The maxmium amount of time the bot will wait between
        // actions.  Will not take any value lower than 15.
        public int MaximumActionGap
        {
            get
            {
                return _MaxActionGap;
            }
            set
            {
                _MaxActionGap = value <= 15 ? 15 : value;
            }
        }


        // Items
        // Items the bot has offered, by itemid.
        public List<ulong> MyOfferedItems
        {
            get
            {
                return _OfferedItemsBuffer;
            }
            set
            {
                _OfferedItemsBuffer = value;
            }
        }

        List<ulong> _OfferedItemsBuffer = new List<ulong> ();
        List<ulong> _OfferedItemsFromSteam = new List<ulong> ();

        // Items the user has offered, by itemid.
        public List<ulong> OtherOfferedItems = new List<ulong> ();

        // The inventory of the bot.
        public Inventory MyInventory;

        // The inventory of the user.
        public Inventory OtherInventory;

        // Internal properties needed for Steam API.
        protected string baseTradeURL;
        protected string steamLogin;
        protected string sessionId;
        protected string apiKey;
        protected int version = 1;
        protected int logpos;
        protected int numEvents;

        protected dynamic OtherItems;
        protected dynamic MyItems;
        #endregion

        #region Events

        /// <summary>
        /// When the trade closes, this is called.  It doesn't matter
        /// whether or not it was a timeout or an error, this is called
        /// to close the trade.
        /// </summary>
        public delegate void CloseHandler ();
        public event CloseHandler OnClose;

        /// <summary>
        /// This is for handling errors that may occur, like inventories
        /// not loading.
        /// </summary>
        public delegate void ErrorHandler (string error);
        public event ErrorHandler OnError;

        /// <summary>
        /// This is for a timeout (either the user didn't complete an
        /// action in a set amount of time, or they took too long with
        /// the whole trade).
        /// </summary>
        public delegate void TimeoutHandler ();
        public event TimeoutHandler OnTimeout;

        /// <summary>
        /// This occurs after Inventories have been loaded.
        /// </summary>
        public delegate void SuccessfulInit ();
        public event SuccessfulInit OnAfterInit;

        /// <summary>
        /// This occurs when the other user adds an item to the trade.
        /// </summary>
        public delegate void UserAddItemHandler (Schema.Item schemaItem, Inventory.Item inventoryItem);
        public event UserAddItemHandler OnUserAddItem;

        /// <summary>
        /// This occurs when the other user removes an item from the 
        /// trade.
        /// </summary>
        public delegate void UserRemoveItemHandler (Schema.Item schemaItem, Inventory.Item inventoryItem);
        public event UserAddItemHandler OnUserRemoveItem;
        
        /// <summary>
        /// This occurs when the user sends a message to the bot over
        /// trade.
        /// </summary>
        public delegate void MessageHandler (string msg);
        public event MessageHandler OnMessage;

        /// <summary>
        /// This occurs when the user sets their ready state to either
        /// true or false.
        /// </summary>
        public delegate void UserSetReadyStateHandler (bool ready);
        public event UserSetReadyStateHandler OnUserSetReady;

        /// <summary>
        /// This occurs when the user accepts the trade.
        /// </summary>
        public delegate void UserAcceptHandler ();
        public event UserAcceptHandler OnUserAccept;
        #endregion

        public Trade (SteamID me, SteamID other, string sessionId, string token, string apiKey, Bot bot)
        {
            MeSID = me;
            OtherSID = other;

            this.sessionId = sessionId;
            steamLogin = token;
            this.apiKey = apiKey;
            this.bot = bot;

            // Moved here because when Poll is called below, these are
            // set to zero, which closes the trade immediately.
            MaximumTradeTime = bot.MaximumTradeTime;
            MaximumActionGap = bot.MaximiumActionGap;

            baseTradeURL = String.Format (SteamTradeUrl, OtherSID.ConvertToUInt64 ());

            // try to poll for the first time
            try
            {
                Poll ();
            }
            catch (Exception)
            {
                bot.log.Error ("[TRADE] Failed To Connect to Steam!");

                if (OnError != null)
                    OnError("There was a problem connecting to Steam Trading.");
            }

            FetchInventories ();
        }
        
        /// <summary>
        /// This updates the trade.  This is called at an interval of a
        /// default of 800ms, not including the execution time of the
        /// method itself.
        /// </summary>
        public void Poll ()
        {

            bot.log.Info ("Polling Trade...");

            if (!tradeStarted)
            {
                tradeStarted = true;
                TradeStart = DateTime.Now;
                LastAction = DateTime.Now;
            }


            StatusObj status = GetStatus ();

            // I've noticed this when the trade is cancelled.
            if (status.trade_status == 3)
            {
                if (OnError != null)
                    OnError ("Trade was cancelled");
                CancelTrade ();
                return;
            }

            if (status.events != null && numEvents != status.events.Length)
            {
                int numLoops = status.events.Length - numEvents;
                numEvents = status.events.Length;

                for (int i = numLoops; i > 0; i--)
                {

                    int EventID;

                    if (numLoops == 1)
                    {
                        EventID = numEvents - 1;
                    }
                    else
                    {
                        EventID = numEvents - i;
                    }

                    bool isBot = status.events [EventID].steamid == MeSID.ConvertToUInt64 ().ToString ();

                    /*
                     *
                     * Trade Action ID's
                     *
                     * 0 = Add item (itemid = "assetid")
                     * 1 = remove item (itemid = "assetid")
                     * 2 = Toggle ready
                     * 3 = Toggle not ready
                     * 4
                     * 5
                     * 6
                     * 7 = Chat (message = "text")
                     *
                     */
                    ulong itemID;

                    switch (status.events [EventID].action)
                    {
                    case 0:
                        itemID = (ulong)status.events [EventID].assetid;

                        if (isBot)
                        {
                            _OfferedItemsFromSteam.Add (itemID);
                            MyOfferedItems = _OfferedItemsFromSteam;
                        }   
                        else
                        {
                            OtherOfferedItems.Add (itemID);
                            Inventory.Item item = OtherInventory.GetItem (itemID);
                            Schema.Item schemaItem = CurrentSchema.GetItem (item.Defindex);
                            OnUserAddItem (schemaItem, item);
                        }

                        break;
                    case 1:
                        itemID = (ulong)status.events [EventID].assetid;

                        if (isBot)
                        {
                            _OfferedItemsFromSteam.Remove (itemID);
                            MyOfferedItems = _OfferedItemsFromSteam;
                        }
                        else
                        {
                            OtherOfferedItems.Remove (itemID);
                            Inventory.Item item = OtherInventory.GetItem (itemID);
                            Schema.Item schemaItem = CurrentSchema.GetItem (item.Defindex);
                            OnUserRemoveItem (schemaItem, item);
                        }

                        break;
                    case 2:
                        if (!isBot)
                        {
                            OtherReady = true;
                            OnUserSetReady (true);
                        }
                        break;
                    case 3:
                        if (!isBot)
                        {
                            OtherReady = false;
                            OnUserSetReady (false);
                        }
                        break;
                    case 4:
                        if (!isBot)
                        {
                            OnUserAccept ();
                        }
                        break;
                    case 7:
                        if (!isBot)
                        {
                            OnMessage (status.events [EventID].text);
                        }
                        break;
                    default:
                        bot.log.Warn ("Unkown Event ID: " + status.events [EventID].action);
                        break;
                    }

                    if (!isBot)
                        LastAction = DateTime.Now;
                }

            } 
            else 
            {
                // check if the user is AFK
                var now = DateTime.Now;

                DateTime actionTimeout = LastAction.AddSeconds (MaximumActionGap);
                int untilActionTimeout = (int) Math.Round ((actionTimeout - now).TotalSeconds);

                DateTime tradeTimeout = TradeStart.AddSeconds (MaximumTradeTime);
                int untilTradeTimeout = (int) Math.Round ((tradeTimeout - now).TotalSeconds);

                if (untilActionTimeout <= 0 || untilTradeTimeout <= 0)
                {
                    if (OnTimeout != null)
                    {
                        OnTimeout ();
                    }
                    CancelTrade ();
                }
                else if (untilActionTimeout <= 15 && untilActionTimeout % 5 == 0)
                {
                    SendMessage ("Are You AFK? The trade will be canceled in " + untilActionTimeout + " seconds if you don't do something.");
                }
            }

            // Update Local Variables
            if (status.them != null)
            {
                OtherReady = status.them.ready == 1 ? true : false;
                MeReady = status.me.ready == 1 ? true : false;
            }


            // Update version
            if (status.newversion)
            {
                version = status.version;
            }

            if (status.logpos != 0)
            {
                logpos = status.logpos;
            }

            bot.log.Info ("Poll Successful.");
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
            data.Add ("logpos", "" + logpos);
            data.Add ("version", "" + version);
            return Fetch (baseTradeURL + "chat", "POST", data);
        }

        /// <summary>
        /// Adds an item by its Defindex.  Adds only one item at a
        /// time, but can be called multiple times during a poll due
        /// to the fact that AddItem keeps track of the items it
        /// adds in MyOfferedItems.
        /// </summary>
        /// <returns>
        /// Returns true if an item was found with the corresponding
        /// defindex, false otherwise.
        /// </returns>
        public bool AddItemByDefindex (int defindex, int slot)
        {
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex (defindex);
            foreach(Inventory.Item item in items)
            {
                if (!(item == null || MyOfferedItems.Contains (item.Id)))
                {
                    AddItem (item.Id, slot);
                    return true;
                }
            }
            return false;
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
            if (MyInventory.GetItem (itemid) == null)
                return false;
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);
            Fetch (baseTradeURL + "additem", "POST", data);
            MyOfferedItems.Add (itemid);
            return true;
        }

        /// <summary>
        /// Finds the last item in the trade that has the defindex
        /// passed to it.  It removes the last item, and can determine
        /// the slot number of the item if you don't give it.  REMEMBER:
        /// the slot number of the item should be the the slot number
        /// of the last instance of the item in the offered items.
        /// The limit on determining the slot number is the assumption
        /// that the items were put in order, i.e. slot 1, slot 2,
        /// slot 3, ..., slot n.
        /// </summary>
        /// <returns>
        /// Returns true if it found a corresponding item; false otherwise.
        /// </returns>
        public bool RemoveItemByDefindex (int defindex, int slot=-1)
        {
            ulong lastItem = 0;
            bool slotGiven = slot == -1 ? true : false;
            if (!slotGiven)
                slot = 0;
            foreach(ulong item in MyOfferedItems)
            {
                if (defindex == MyInventory.GetItem (item).Defindex)
                {
                    lastItem = item;
                    if (!slotGiven)
                        slot++;
                }
            }
            if (lastItem != 0)
            {
                RemoveItem (lastItem, slot);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes an item by its itemid.  Read AddItem about itemids.
        /// Returns false if the item isn't in the offered items, or
        /// true if it appears it succeeded.  Removes the item from
        /// MyOfferedItems.
        /// </summary>
        public bool RemoveItem (ulong itemid, int slot)
        {
            if (!MyOfferedItems.Contains (itemid))
                return false;
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);
            Fetch (baseTradeURL + "removeitem", "POST", data);
            MyOfferedItems.Remove (itemid);
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
            bot.log.Error ("CANCELED TRADE");
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            Fetch (baseTradeURL + "cancel", "POST", data);
            if (OnClose != null)
                OnClose ();
        }
        #endregion

        protected StatusObj GetStatus ()
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("logpos", "" + logpos);
            data.Add ("version", "" + version);

            string response = Fetch (baseTradeURL + "tradestatus", "POST", data);
            return JsonConvert.DeserializeObject<StatusObj> (response);
        }

        protected dynamic GetInventory (SteamID steamid)
        {
            string url = String.Format (
                "http://steamcommunity.com/profiles/{0}/inventory/json/440/2/?trading=1",
                steamid.ConvertToUInt64 ()
            );

            try
            {
                string response = Fetch (url, "GET", null, false);
                return JsonConvert.DeserializeObject (response);
            }
            catch (Exception)
            {
                return JsonConvert.DeserializeObject ("{\"success\":\"false\"}");
            }
        }

        protected string Fetch (string url, string method, NameValueCollection data = null, bool sendLoginData = true)
        {
            var cookies = new CookieContainer();
            if (sendLoginData)
            {
                cookies.Add (new Cookie ("sessionid", sessionId, String.Empty, SteamCommunityDomain));
                cookies.Add (new Cookie ("steamLogin", steamLogin, String.Empty, SteamCommunityDomain));
            }

            return SteamWeb.Fetch (url, method, data, cookies);
        }

        /// <summary>
        /// Grabs the inventories of both users over both Trading and
        /// SteamAPI.
        /// </summary>
        protected void FetchInventories ()
        {
            try
            {
                // fetch the other player's inventory
                OtherItems = GetInventory (OtherSID);
                if (OtherItems == null || OtherItems.success != "true")
                {
                    throw new Exception ("Could not fetch other player's inventory via Trading!");
                }

                // fetch our inventory
                MyItems = GetInventory (MeSID);
                if (MyItems == null || MyItems.success != "true")
                {
                    throw new Exception ("Could not fetch own inventory via Trading!");
                }

                // fetch other player's inventory from the Steam API.
                OtherInventory = Inventory.FetchInventory(OtherSID.ConvertToUInt64(), apiKey);
                if (OtherInventory == null)
                {
                    throw new Exception ("Could not fetch other player's inventory via Steam API!");
                }

                // fetch our inventory from the Steam API.
                MyInventory = Inventory.FetchInventory(MeSID.ConvertToUInt64(), apiKey);
                if (MyInventory == null)
                {
                    throw new Exception ("Could not fetch own inventory via Steam API!");
                }

                // check that the schema was already successfully fetched
                if (CurrentSchema == null)
                {
                    throw new Exception ("It seems the item schema was not fetched correctly!");
                }

                if (OnAfterInit != null)
                    OnAfterInit();

            }
            catch (Exception e)
            {
                if (OnError != null)
                    OnError ("I'm having a problem getting one of our backpacks. The Steam Community might be down. Ensure your backpack isn't private.");
                bot.log.Error (e.ToString ());
            }
        }

        #region JSON classes
        protected class StatusObj
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

        protected class TradeEvent
        {
            public string steamid { get; set; }

            public int action { get; set; }

            public ulong timestamp { get; set; }

            public int appid { get; set; }

            public string text { get; set; }

            public int contextid { get; set; }

            public ulong assetid { get; set; }
        }

        protected class TradeUserObj
        {
            public int ready { get; set; }

            public int confirmed { get; set; }

            public int sec_since_touch { get; set; }
        }
        #endregion

    }
}

