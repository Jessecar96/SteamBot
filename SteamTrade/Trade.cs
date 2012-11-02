using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade
{
    public class Trade
    {
        #region Static Public data
        public static Schema CurrentSchema = null;
        #endregion

        // current bot's sid
        private SteamID MeSID;

        private Log log;

        // If the bot is ready.
        private bool MeReady = false;

        // If the other user is ready.
        private bool OtherReady = false;

        // Whether or not the trade actually started.
        private bool tradeStarted = false;

        // When the trade started.
        private DateTime TradeStart;

        // When the last action taken by the user was.
        private DateTime LastAction;

        private int _MaxTradeTime;
        private int _MaxActionGap;

        //private List<ulong> _OfferedItemsBuffer = new List<ulong> ();
        //private List<ulong> _OfferedItemsFromSteam = new List<ulong> ();

        // The inventory of the bot.
        private Inventory MyInventory;

        // Internal properties needed for Steam API.
        private string apiKey;
        private int numEvents;

        private dynamic OtherItems;
        private dynamic MyItems;

        private TradeSession tradeSession;

        public Trade (SteamID me, SteamID other, string sessionId, string token, string apiKey, int maxTradeTime, int maxGapTime, Log log)
        {
            MeSID = me;
            OtherSID = other;

            tradeSession = new TradeSession(sessionId, token, OtherSID);

            this.apiKey = apiKey;
            this.log = log;

            // Moved here because when Poll is called below, these are
            // set to zero, which closes the trade immediately.
            MaximumTradeTime = maxTradeTime;
            MaximumActionGap = maxGapTime;

            OtherOfferedItems = new List<ulong> ();
            MyOfferedItems = new List<ulong> ();

            // try to poll for the first time
            try
            {
                Poll ();
            }
            catch (Exception)
            {
                log.Error ("[TRADE] Failed To Connect to Steam!");

                if (OnError != null)
                    OnError("There was a problem connecting to Steam Trading.");
            }

            FetchInventories ();
        }

        #region Public Properties

        /// <summary>Gets or sets the other user's steam ID.</summary> 
        public SteamID OtherSID { get; private set; }

        /// <summary>
        /// Gets or sets The maximum trading time the bot will take.  Will not take any value lower than 15.
        /// </summary>
        /// <value>
        /// The maximum trade time.
        /// </value>
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

        /// <summary>
        /// Gets or sets The maxmium amount of time the bot will wait between actions. 
        /// Will not take any value lower than 15.
        /// </summary>
        /// <value>
        /// The maximum action gap.
        /// </value>
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
        
        /// <summary>
        /// Gets or sets the list of items (itemids) the bot has offered.
        /// </summary>
        /// <value>
        /// My offered items.
        /// </value>
        public List<ulong> MyOfferedItems { get; private set; }

        /// <summary> 
        /// Gets the inventory of the other user. 
        /// </summary>
        public Inventory OtherInventory { get; private set; }
        
        /// <summary>
        /// Gets the items the user has offered, by itemid.
        /// </summary>
        /// <value>
        /// The other offered items.
        /// </value>
        public List<ulong> OtherOfferedItems { get; private set; }

        #endregion
                
        #region Public Events

        public delegate void CloseHandler ();
        public delegate void ErrorHandler (string error);
        public delegate void TimeoutHandler ();
        public delegate void SuccessfulInit ();
        public delegate void UserAddItemHandler (Schema.Item schemaItem, Inventory.Item inventoryItem);
        public delegate void UserRemoveItemHandler (Schema.Item schemaItem, Inventory.Item inventoryItem);
        public delegate void MessageHandler (string msg);
        public delegate void UserSetReadyStateHandler (bool ready);
        public delegate void UserAcceptHandler ();

        /// <summary>
        /// When the trade closes, this is called.  It doesn't matter
        /// whether or not it was a timeout or an error, this is called
        /// to close the trade.
        /// </summary>
        public event CloseHandler OnClose;
        
        /// <summary>
        /// This is for handling errors that may occur, like inventories
        /// not loading.
        /// </summary>
        public event ErrorHandler OnError;

        /// <summary>
        /// This is for a timeout (either the user didn't complete an
        /// action in a set amount of time, or they took too long with
        /// the whole trade).
        /// </summary>
        public event TimeoutHandler OnTimeout;

        /// <summary>
        /// This occurs after Inventories have been loaded.
        /// </summary>
        public event SuccessfulInit OnAfterInit;

        /// <summary>
        /// This occurs when the other user adds an item to the trade.
        /// </summary>
        public event UserAddItemHandler OnUserAddItem;
        
        /// <summary>
        /// This occurs when the other user removes an item from the 
        /// trade.
        /// </summary>
        public event UserAddItemHandler OnUserRemoveItem;

        /// <summary>
        /// This occurs when the user sends a message to the bot over
        /// trade.
        /// </summary>
        public event MessageHandler OnMessage;

        /// <summary>
        /// This occurs when the user sets their ready state to either
        /// true or false.
        /// </summary>
        public event UserSetReadyStateHandler OnUserSetReady;

        /// <summary>
        /// This occurs when the user accepts the trade.
        /// </summary>
        public event UserAcceptHandler OnUserAccept;
        
        #endregion

        /// <summary>
        /// Cancel the trade.  This calls the OnClose handler, as well.
        /// </summary>
        public void CancelTrade ()
        {
            log.Error ("CANCELED TRADE");
            
            tradeSession.CancelTrade();
            
            if (OnClose != null)
                OnClose ();
        }

        /// <summary>
        /// Sends a message to the user over the trade chat.
        /// </summary>
        public string SendMessage (string msg)
        {
            return tradeSession.SendMessage(msg);
        }

        /// <summary>
        /// Sets the bot to a ready status.
        /// </summary>
        public void SetReady (bool ready)
        {
            tradeSession.SetReady(ready);
        }

        /// <summary>
        /// Accepts the trade from the user.  Returns a deserialized
        /// JSON object.
        /// </summary>
        public dynamic AcceptTrade ()
        {
            return tradeSession.AcceptTrade();
        }
        
        /// <summary>
        /// This updates the trade.  This is called at an interval of a
        /// default of 800ms, not including the execution time of the
        /// method itself.
        /// </summary>
        public void Poll ()
        {
            log.Info ("Polling Trade...");

            if (!tradeStarted)
            {
                tradeStarted = true;
                TradeStart = DateTime.Now;
                LastAction = DateTime.Now;
            }


            TradeSession.StatusObj status = tradeSession.GetStatus ();

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
                            //_OfferedItemsFromSteam.Add (itemID);
                            //MyOfferedItems = _OfferedItemsFromSteam;
                            MyOfferedItems.Add (itemID);
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
                            //_OfferedItemsFromSteam.Remove (itemID);
                            //MyOfferedItems = _OfferedItemsFromSteam;
                            MyOfferedItems.Remove (itemID);
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
                        log.Warn ("Unkown Event ID: " + status.events [EventID].action);
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
                    tradeSession.SendMessage ("Are You AFK? The trade will be canceled in " + untilActionTimeout + " seconds if you don't do something.");
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
                tradeSession.Version = status.version;
            }

            if (status.logpos != 0)
            {
                tradeSession.LogPos = status.logpos;
            }

            log.Info ("Poll Successful.");
        }


        /// <summary>
        /// Grabs the inventories of both users over both Trading and
        /// SteamAPI.
        /// </summary>
        protected void FetchInventories ()
        {
            try
            {
                // [cmw] OtherItems and MyItems don't appear to be used... the should be removed.
                // fetch the other player's inventory
                OtherItems = Inventory.GetInventory (OtherSID);
                if (OtherItems == null || OtherItems.success != "true")
                {
                    throw new Exception ("Could not fetch other player's inventory via Trading!");
                }

                // fetch our inventory
                MyItems = Inventory.GetInventory (MeSID);
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
                log.Error (e.ToString ());
            }
        }
    }
}

