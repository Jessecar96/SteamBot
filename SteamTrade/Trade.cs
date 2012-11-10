using System;
using System.Collections.Generic;
using SteamKit2;
using SteamTrade.Exceptions;

namespace SteamTrade
{
    public partial class Trade
    {
        #region Static Public data
        public static Schema CurrentSchema = null;
        #endregion

        // current bot's sid
        SteamID mySteamId;

        // If the bot is ready.
        bool meIsReady = false;

        // If the other user is ready.
        bool otherIsReady = false;

        // Whether or not the trade actually started.
        bool tradeStarted = false;

        // When the trade started.
        DateTime tradeStartTime;

        // When the last action taken by the user was.
        DateTime lastOtherActionTime;
        int _MaxTradeTime;
        int _MaxActionGap;
        Dictionary<int, ulong> myOfferedItems;
        List<ulong> steamMyOfferedItems;

        // The inventory of the bot.
        Inventory myInventory;

        // Internal properties needed for Steam API.
        string apiKey;
        int numEvents;
        dynamic othersItems;
        dynamic myItems;

        public Trade (SteamID me, SteamID other, string sessionId, string token, string apiKey, int maxTradeTime, int maxGapTime)
        {
            mySteamId = me;
            OtherSID = other;
            this.sessionId = sessionId;
            this.steamLogin = token;
            this.apiKey = apiKey;
            
            // Moved here because when Poll is called below, these are
            // set to zero, which closes the trade immediately.
            MaximumTradeTime = maxTradeTime;
            MaximumActionGap = maxGapTime;

            OtherOfferedItems = new List<ulong> ();
            myOfferedItems = new Dictionary<int, ulong> ();
            steamMyOfferedItems = new List<ulong> ();

            Init ();

            // try to poll for the first time
            try
            {
                Poll ();
            } catch (Exception e)
            {
                if (OnError != null)
                    OnError ("There was a problem connecting to Steam Trading.");
                else 
                    throw new TradeException ("Unhandled exception when Polling the trade.", e);
            }

            FetchInventories ();
            sessionIdEsc = Uri.UnescapeDataString (this.sessionId);
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

        /// <summary>
        /// Gets a value indicating if the remote trading partner cancelled the trade.
        /// </summary>
        public bool OtherUserCancelled { get; private set; }

        #endregion
                
        #region Public Events

        public delegate void CloseHandler ();

        public delegate void ErrorHandler (string error);

        public delegate void TimeoutHandler ();

        public delegate void SuccessfulInit ();

        public delegate void UserAddItemHandler (Schema.Item schemaItem,Inventory.Item inventoryItem);

        public delegate void UserRemoveItemHandler (Schema.Item schemaItem,Inventory.Item inventoryItem);

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
        public bool CancelTrade ()
        {
            bool ok = CancelTradeWebCmd ();

            if (!ok)
                throw new TradeException ("The Web command to cancel the trade failed");
            
            if (OnClose != null)
                OnClose ();

            return true;
        }

        /// <summary>
        /// Adds a specified item by its itemid.
        /// </summary>
        /// <returns><c>false</c> if the item was not found in the inventory.</returns>
        public bool AddItem (ulong itemid)
        {
            if (myInventory.GetItem (itemid) == null)
                return false;

            var slot = NextTradeSlot ();
            bool ok = AddItemWebCmd (itemid, slot);

            if (!ok)
                throw new TradeException ("The Web command to add the Item failed");

            myOfferedItems [slot] = itemid;

            return true;
        }

        /// <summary>
        /// Adds a single item by its Defindex.
        /// </summary>
        /// <returns>
        /// <c>true</c> if an item was found with the corresponding
        /// defindex, <c>false</c> otherwise.
        /// </returns>
        public bool AddItemByDefindex (int defindex)
        {
            List<Inventory.Item> items = myInventory.GetItemsByDefindex (defindex);
            foreach (Inventory.Item item in items)
            {
                if (!myOfferedItems.ContainsValue (item.Id))
                {
                    return AddItem (item.Id);
                }
            }
            return false;
        }

        /// <summary>
        /// Removes an item by its itemid.
        /// </summary>
        /// <returns><c>false</c> the item was not found in the trade.</returns>
        public bool RemoveItem (ulong itemid)
        {
            int? slot = GetItemSlot (itemid);
            if (!slot.HasValue)
                return false;

            bool ok = RemoveItemWebCmd (itemid, slot.Value);

            if (!ok)
                throw new TradeException ("The web command to remove the item failed.");

            myOfferedItems.Remove (slot.Value);

            return true;
        }

        /// <summary>
        /// Removes an item with the given Defindex from the trade.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if it found a corresponding item; <c>false</c> otherwise.
        /// </returns>
        public bool RemoveItemByDefindex (int defindex)
        {
            foreach (ulong id in myOfferedItems.Values)
            {
                Inventory.Item item = myInventory.GetItem (id);
                if (item.Defindex == defindex)
                {
                    return RemoveItem (item.Id);
                }
            }
            return false;
        }

        /// <summary>
        /// Sends a message to the user over the trade chat.
        /// </summary>
        public bool SendMessage (string msg)
        {
            bool ok = SendMessageWebCmd (msg);

            if (!ok)
                throw new TradeException ("The web command to send the trade message failed.");

            return true;
        }

        /// <summary>
        /// Sets the bot to a ready status.
        /// </summary>
        public bool SetReady (bool ready)
        {
            bool ok = SetReadyWebCmd (ready);

            if (!ok)
                throw new TradeException ("The web command to set trade ready state failed.");

            return true;
        }

        /// <summary>
        /// Accepts the trade from the user.  Returns a deserialized
        /// JSON object.
        /// </summary>
        public bool AcceptTrade ()
        {
            bool ok = AcceptTradeWebCmd ();

            if (!ok)
                throw new TradeException ("The web command to accept the trade failed.");

            return true;
        }
        
        /// <summary>
        /// This updates the trade.  This is called at an interval of a
        /// default of 800ms, not including the execution time of the
        /// method itself.
        /// </summary>
        public void Poll ()
        {
            if (!TradeStarted)
            {
                tradeStarted = true;
                tradeStartTime = DateTime.Now;
                lastOtherActionTime = DateTime.Now;
            }

            StatusObj status = GetStatus ();

            if (status == null)
                throw new TradeException ("The web command to get the trade status failed.");

            // I've noticed this when the trade is cancelled.
            if (status.trade_status == 3)
            {
                if (OnError != null)
                    OnError ("Trade was cancelled by other user.");

                OtherUserCancelled = true;
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
                    } else
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
                            steamMyOfferedItems.Add (itemID);
                        } else
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
                            steamMyOfferedItems.Remove (itemID);
                        } else
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
                        // Todo: add an OnWarning or similar event
                        if (OnError != null)
                            OnError ("Unkown Event ID: " + status.events [EventID].action);
                        break;
                    }

                    if (!isBot)
                        lastOtherActionTime = DateTime.Now;
                }

                ValidateLocalTradeItems ();

            } else
            {
                // check if the user is AFK
                var now = DateTime.Now;

                DateTime actionTimeout = lastOtherActionTime.AddSeconds (MaximumActionGap);
                int untilActionTimeout = (int)Math.Round ((actionTimeout - now).TotalSeconds);

                DateTime tradeTimeout = TradeStartTime.AddSeconds (MaximumTradeTime);
                int untilTradeTimeout = (int)Math.Round ((tradeTimeout - now).TotalSeconds);

                if (untilActionTimeout <= 0 || untilTradeTimeout <= 0)
                {
                    if (OnTimeout != null)
                    {
                        OnTimeout ();
                    }
                    CancelTrade ();
                } else if (untilActionTimeout <= 15 && untilActionTimeout % 5 == 0)
                {
                    SendMessageWebCmd ("Are You AFK? The trade will be canceled in " + untilActionTimeout + " seconds if you don't do something.");
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
                Version = status.version;
            }

            if (status.logpos != 0)
            {
                LogPos = status.logpos;
            }
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
                OtherInventory = Inventory.FetchInventory (OtherSID.ConvertToUInt64 (), apiKey);
                if (OtherInventory == null)
                {
                    throw new Exception ("Could not fetch other player's inventory via Steam API!");
                }

                // fetch our inventory from the Steam API.
                myInventory = Inventory.FetchInventory (MySteamId.ConvertToUInt64 (), apiKey);
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
                    OnAfterInit ();

            } catch (Exception e)
            {
                if (OnError != null)
                    OnError ("I'm having a problem getting one of our backpacks. The Steam Community might be down. Ensure your backpack isn't private.");
                else
                    throw new TradeException ("I'm having a problem getting one of our backpacks. The Steam Community might be down. Ensure your backpack isn't private.", e);
            }
        }

        int NextTradeSlot ()
        {
            int slot = 0;
            while (myOfferedItems.ContainsKey (slot))
            {
                slot++;
            }
            return slot;
        }

        int? GetItemSlot (ulong itemid)
        {
            foreach (int slot in myOfferedItems.Keys)
            {
                if (myOfferedItems [slot] == itemid)
                {
                    return slot;
                }
            }
            return null;
        }

        void ValidateLocalTradeItems ()
        {
            if (myOfferedItems.Count != steamMyOfferedItems.Count)
                goto error;
            foreach (ulong id in myOfferedItems.Values)
            {
                if (!steamMyOfferedItems.Contains (id))
                    goto error;
            }
            return;
            error:
            throw new Exception ("Error validating local copy of items in the trade!");
        }
    }
}

