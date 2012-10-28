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

        // This has been replaced in favor of the class Log, as 1) Log writes to files,
        // and 2) Log has varying levels.
        /*protected static void PrintConsole (String line, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine (line);
            Console.ForegroundColor = ConsoleColor.White;
        }*/
        #endregion

        #region Properties
        public SteamID MeSID;
        public SteamID OtherSID;
        public Bot bot;

        // Generic Trade info
        public bool MeReady = false;
        public bool OtherReady = false;

        bool tradeStarted = false;
        public DateTime TradeStart;
        public DateTime LastAction;
        private int _MaxTradeTime;
        private int _MaxActionGap;

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
        public List<ulong> MyOfferedItems = new List<ulong> ();
        public List<ulong> OtherOfferedItems = new List<ulong> ();

        public Inventory OtherInventory;
        public Inventory MyInventory;

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
        public delegate void ErrorHandler (string error);
        public event ErrorHandler OnError;

        public delegate void TimeoutHandler ();
        public event TimeoutHandler OnTimeout;

        public delegate void SuccessfulInit ();
        public event SuccessfulInit OnAfterInit;

        public delegate void UserAddItemHandler (Schema.Item schemaItem, Inventory.Item inventoryItem);
        public event UserAddItemHandler OnUserAddItem;

        public delegate void UserRemoveItemHandler (Schema.Item schemaItem, Inventory.Item inventoryItem);
        public event UserAddItemHandler OnUserRemoveItem;

        public delegate void MessageHandler (string msg);
        public event MessageHandler OnMessage;

        public delegate void UserSetReadyStateHandler (bool ready);
        public event UserSetReadyStateHandler OnUserSetReady;

        public delegate void UserAcceptHandler ();
        public event UserAcceptHandler OnUserAccept;
        #endregion

        public Trade (SteamID me, SteamID other, string sessionId, string token, string apiKey, Bot bot, TradeListener listener = null/*, int maxtradetime = 180, int maxactiongap = 30*/)
        {
            MeSID = me;
            OtherSID = other;

            this.sessionId = sessionId;
            steamLogin = token;
            this.apiKey = apiKey;
            //this.MaximumTradeTime = maxtradetime <= 15 ? 15 : maxtradetime;             // Set a minimium time of 15 seconds
            //this.MaximumActionGap = maxactiongap <= 15 ? 15 : maxactiongap;             // Again, minimium time of 15 seconds
            AddListener (listener);

            baseTradeURL = String.Format (SteamTradeUrl, OtherSID.ConvertToUInt64 ());

            // try to poll for the first time
            try
            {
                Poll ();
            }
            catch (Exception)
            {
                bot.log.Error ("[TRADE] Failed To Connect to Steam!");
                //PrintConsole ("Failed to connect to Steam!", ConsoleColor.Red);

                if (OnError != null)
                    OnError("There was a problem connecting to Steam Trading.");
            }

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
                Console.WriteLine (e);
            }

        }

        public void Poll ()
        {
            if (!tradeStarted)
            {
                tradeStarted = true;
                TradeStart = DateTime.Now;
                LastAction = DateTime.Now;
            }


            StatusObj status = GetStatus ();

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
                            MyOfferedItems.Add (itemID);
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
                            MyOfferedItems.Remove (itemID);
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
                        //PrintConsole ("Unknown Event ID: " + status.events [EventID].action, ConsoleColor.Red);
                        break;
                    }

                    if (!isBot)
                        LastAction = DateTime.Now;
                }

            } else {
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
                        OnTimeout();
                    }
                    CancelTrade();
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
        }

        #region Trade interaction
        public string SendMessage (string msg)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("message", msg);
            data.Add ("logpos", "" + logpos);
            data.Add ("version", "" + version);
            return Fetch (baseTradeURL + "chat", "POST", data);
        }

        public bool AddItemByDefindex (int defindex, int slot)
        {
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex (defindex);
            if (items[0] != null)
            {
                AddItem (items[0].Id, slot);
                return true;
            }
            return false;
        }

        public void AddItem (ulong itemid, int slot)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);
            Fetch (baseTradeURL + "additem", "POST", data);
        }

        public void RemoveItem (ulong itemid, int slot)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);
            Fetch (baseTradeURL + "removeitem", "POST", data);
        }

        public void SetReady (bool ready)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("ready", ready ? "true" : "false");
            data.Add ("version", "" + version);
            Fetch (baseTradeURL + "toggleready", "POST", data);
        }

        public dynamic AcceptTrade ()
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
            data.Add ("version", "" + version);
            string response = Fetch (baseTradeURL + "confirm", "POST", data);

            return JsonConvert.DeserializeObject (response);
        }

        public void CancelTrade ()
        {
                var data = new NameValueCollection ();
                data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
                Fetch (baseTradeURL + "cancel", "POST", data);
        }
        #endregion

        public void AddListener (TradeListener listener)
        {
            OnError += listener.OnError;
            OnTimeout += listener.OnTimeout;
            OnAfterInit += listener.OnAfterInit;
            OnUserAddItem += listener.OnUserAddItem;
            OnUserRemoveItem += listener.OnUserRemoveItem;
            OnMessage += listener.OnMessage;
            OnUserSetReady += listener.OnUserSetReadyState;
            OnUserAccept += listener.OnUserAccept;
            listener.trade = this;
        }

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

        public abstract class TradeListener
        {
            public Trade trade;

            public abstract void OnError (string error);

            public abstract void OnTimeout ();

            public abstract void OnAfterInit ();

            public abstract void OnUserAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem);

            public abstract void OnUserRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem);

            public abstract void OnMessage (string msg);

            public abstract void OnUserSetReadyState (bool ready);

            public abstract void OnUserAccept ();
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

