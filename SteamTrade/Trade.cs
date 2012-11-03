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
        private SteamID mySteamId;

        private Log log;

        // If the bot is ready.
        private bool meIsReady = false;

        // If the other user is ready.
        private bool otherIsReady = false;

        // Whether or not the trade actually started.
        private bool tradeStarted = false;

        // When the trade started.
        private DateTime tradeStartTime;

        // When the last action taken by the user was.
        private DateTime lastOtherActionTime;

        private int _MaxTradeTime;
        private int _MaxActionGap;

        // The inventory of the bot.
        private Inventory myInventory;

        // Internal properties needed for Steam API.
        private string apiKey;
        private int numEvents;

        private dynamic othersItems;
        private dynamic myItems;

        private TradeSession tradeSession;

        public Trade (SteamID me, SteamID other, string sessionId, string token, string apiKey, int maxTradeTime, int maxGapTime, Log log)
        {
            mySteamId = me;
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

        /// <summary>Gets the other user's steam ID.</summary> 
        public SteamID OtherSID { get; private set; }

        /// <summary>
        /// Gets the bot's Steam ID.
        /// </summary>
        public SteamID MySteamId
        {
            get { return mySteamId; }
        }

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
        /// Gets the list of items (itemids) the bot has offered.
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


        /// <summary>
        /// Gets a value indicating if the other user is ready to trade.
        /// </summary>
        public bool OtherIsReady
        {
            get { return otherIsReady; }
        }

        /// <summary>
        /// Gets a value indicating if the bot is ready to trade.
        /// </summary>
        public bool MeIsReady
        {
            get { return meIsReady; }
        }

        /// <summary>
        /// Gets the time the trade started.
        /// </summary>
        public DateTime TradeStartTime
        {
            get { return tradeStartTime; }
        }

        /// <summary>
        /// Gets a value indicating if a trade has started.
        /// </summary>
        public bool TradeStarted
        {
            get { return tradeStarted; }
        }

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
        /// Adds an item by its Defindex. Adds only one item at a
        /// time, but can be called multiple times.
        /// </summary>
        /// <param name="defindex">The item defindex (item type really).</param>
        /// <param name="slot">The trade slot index to place the item in.</param>
        /// <returns>
        /// true if an item was found with the corresponding
        /// defindex, false otherwise.
        /// </returns>
        public bool AddItemByDefindex(int defindex, int slot)
        {
            List<Inventory.Item> items = myInventory.GetItemsByDefindex(defindex);

            foreach (Inventory.Item item in items)
            {
                if (!(item == null || MyOfferedItems.Contains(item.Id)))
                {
                    tradeSession.AddItem(item.Id, slot);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the last item in the trade that has the defindex
        /// passed to it. It removes the last item, and can determine
        /// the slot number of the item if you don't give it. REMEMBER:
        /// the slot number of the item should be the the slot number
        /// of the last instance of the item in the offered items.
        /// The limit on determining the slot number is the assumption
        /// that the items were put in order, i.e. slot 1, slot 2,
        /// slot 3, ..., slot n.
        /// </summary>
        /// <returns>
        /// Returns true if it found a corresponding item; false otherwise.
        /// </returns>
        public bool RemoveItemByDefindex(int defindex, int slot = -1)
        {
            ulong lastItem = 0;

            bool slotGiven = slot == -1 ? true : false;

            if (!slotGiven)
                slot = 0;

            foreach (ulong item in MyOfferedItems)
            {
                if (defindex == myInventory.GetItem(item).Defindex)
                {
                    lastItem = item;
                    if (!slotGiven)
                        slot++;
                }
            }

            if (lastItem != 0)
            {
                tradeSession.RemoveItem(lastItem, slot);
                return true;
            }

            return false;
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

            if (!TradeStarted)
            {
                tradeStarted = true;
                tradeStartTime = DateTime.Now;
                lastOtherActionTime = DateTime.Now;
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

                    bool isBot = status.events [EventID].steamid == MySteamId.ConvertToUInt64 ().ToString ();

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
                            otherIsReady = true;
                            OnUserSetReady (true);
                        }
                        break;
                    case 3:
                        if (!isBot)
                        {
                            otherIsReady = false;
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
                        lastOtherActionTime = DateTime.Now;
                }

            } 
            else 
            {
                // check if the user is AFK
                var now = DateTime.Now;

                DateTime actionTimeout = lastOtherActionTime.AddSeconds (MaximumActionGap);
                int untilActionTimeout = (int) Math.Round ((actionTimeout - now).TotalSeconds);

                DateTime tradeTimeout = TradeStartTime.AddSeconds (MaximumTradeTime);
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
                otherIsReady = status.them.ready == 1 ? true : false;
                meIsReady = status.me.ready == 1 ? true : false;
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
                othersItems = Inventory.GetInventory (OtherSID);
                if (othersItems == null || othersItems.success != "true")
                {
                    throw new Exception ("Could not fetch other player's inventory via Trading!");
                }

                // fetch our inventory
                myItems = Inventory.GetInventory (MySteamId);
                if (myItems == null || myItems.success != "true")
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
                myInventory = Inventory.FetchInventory(MySteamId.ConvertToUInt64(), apiKey);
                if (myInventory == null)
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

