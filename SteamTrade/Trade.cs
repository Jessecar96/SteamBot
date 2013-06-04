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

        // list to store all trade events already processed
        List<TradeEvent> eventList;

        // current bot's sid
        SteamID mySteamId;

        // If the bot is ready.
        bool meIsReady = false;

        // If the other user is ready.
        bool otherIsReady = false;

        // Whether or not the trade actually started.
        bool tradeStarted = false;

        Dictionary<int, ulong> myOfferedItems;
        List<ulong> steamMyOfferedItems;

        // Internal properties needed for Steam API.
        string apiKey;
        int numEvents;

        internal Trade (SteamID me, SteamID other, string sessionId, string token, string apiKey, Inventory myInventory, Inventory otherInventory)
        {
            mySteamId = me;
            OtherSID = other;
            this.sessionId = sessionId;
            this.steamLogin = token;
            this.apiKey = apiKey;
            this.eventList = new List<TradeEvent>();

            OtherOfferedItems = new List<ulong> ();
            myOfferedItems = new Dictionary<int, ulong> ();
            steamMyOfferedItems = new List<ulong> ();

            OtherInventory = otherInventory;
            MyInventory = myInventory;

            Init ();
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
        /// Gets the inventory of the other user. 
        /// </summary>
        public Inventory OtherInventory { get; private set; }

        /// <summary> 
        /// Gets the inventory of the bot.
        /// </summary>
        public Inventory MyInventory { get; private set; }

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

        /// <summary>
        /// Gets a value indicating whether the trade completed normally. This
        /// is independent of other flags.
        /// </summary>
        public bool HasTradeCompletedOk { get; private set; }

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
            if (MyInventory.GetItem (itemid) == null)
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
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex (defindex);
            foreach (Inventory.Item item in items)
            {
                if (item != null && !myOfferedItems.ContainsValue(item.Id) && !item.IsNotTradeable)
                {
                    return AddItem (item.Id);
                }
            }
            return false;
        }

        /// <summary>
        /// Adds an entire set of items by Defindex to each successive
        /// slot in the trade.
        /// </summary>
        /// <param name="defindex">The defindex. (ex. 5022 = crates)</param>
        /// <param name="numToAdd">The upper limit on amount of items to add. <c>0</c> to add all items.</param>
        /// <returns>Number of items added.</returns>
        public uint AddAllItemsByDefindex (int defindex, uint numToAdd = 0)
        {
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex (defindex);

            uint added = 0;

            foreach (Inventory.Item item in items)
            {
                if (item != null && !myOfferedItems.ContainsValue(item.Id) && !item.IsNotTradeable)
                {
                    bool success = AddItem (item.Id);

                    if (success)
                        added++;

                    if (numToAdd > 0 && added >= numToAdd)
                        return added;
                }
            }

            return added;
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
                Inventory.Item item = MyInventory.GetItem (id);
                if (item.Defindex == defindex)
                {
                    return RemoveItem (item.Id);
                }
            }
            return false;
        }

        /// <summary>
        /// Removes an entire set of items by Defindex.
        /// </summary>
        /// <param name="defindex">The defindex. (ex. 5022 = crates)</param>
        /// <param name="numToRemove">The upper limit on amount of items to remove. <c>0</c> to remove all items.</param>
        /// <returns>Number of items removed.</returns>
        public uint RemoveAllItemsByDefindex (int defindex, uint numToRemove = 0)
        {
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex (defindex);

            uint removed = 0;

            foreach (Inventory.Item item in items)
            {
                if (item != null && myOfferedItems.ContainsValue (item.Id))
                {
                    bool success = RemoveItem (item.Id);

                    if (success)
                        removed++;

                    if (numToRemove > 0 && removed >= numToRemove)
                        return removed;
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes all offered items from the trade.
        /// </summary>
        /// <returns>Number of items removed.</returns>
        public uint RemoveAllItems()
        {
            uint removed = 0;

            var copy = new Dictionary<int, ulong>(myOfferedItems);

            foreach (var id in copy)
            {
                Inventory.Item item = MyInventory.GetItem(id.Value);

                bool success = RemoveItem(item.Id);

                if (success)
                    removed++;
            }

            return removed;
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
            // testing
            ValidateLocalTradeItems ();

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
            ValidateLocalTradeItems ();

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
        /// <returns><c>true</c> if the other trade partner performed an action; otherwise <c>false</c>.</returns>
        public bool Poll ()
        {
            bool otherDidSomething = false;

            if (!TradeStarted)
            {
                tradeStarted = true;

                // since there is no feedback to let us know that the trade
                // is fully initialized we assume that it is when we start polling.
                if (OnAfterInit != null)
                    OnAfterInit ();
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
                return otherDidSomething;
            }

            if (status.trade_status == 1)
            {
                // trade completed successfully.
                HasTradeCompletedOk = true;
                return otherDidSomething;
            }


            if (status.events != null)
            {
                foreach (TradeEvent trdEvent in status.events)
                {
                    if (!eventList.Contains(trdEvent))
                    {
                        eventList.Add(trdEvent);//add event to processed list, as we are taking care of this event now
                        bool isBot = trdEvent.steamid == MySteamId.ConvertToUInt64().ToString();

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

                        switch ((TradeEventType)trdEvent.action)
                        {
                            case TradeEventType.ItemAdded:
                                itemID = (ulong)trdEvent.assetid;

                                if (isBot)
                                {
                                    steamMyOfferedItems.Add(itemID);
                                    ValidateSteamItemChanged(itemID, true);
                                    Inventory.Item item = MyInventory.GetItem(itemID);
                                    Schema.Item schemaItem = CurrentSchema.GetItem(item.Defindex);
                                }
                                else
                                {
                                    OtherOfferedItems.Add(itemID);
                                    Inventory.Item item = OtherInventory.GetItem(itemID);
                                    Schema.Item schemaItem = CurrentSchema.GetItem(item.Defindex);
                                    OnUserAddItem(schemaItem, item);
                                }

                                break;
                            case TradeEventType.ItemRemoved:
                                itemID = (ulong)trdEvent.assetid;

                                if (isBot)
                                {
                                    steamMyOfferedItems.Remove(itemID);
                                    ValidateSteamItemChanged(itemID, false);
                                    Inventory.Item item = MyInventory.GetItem(itemID);
                                    Schema.Item schemaItem = CurrentSchema.GetItem(item.Defindex);
                                }
                                else
                                {
                                    OtherOfferedItems.Remove(itemID);
                                    Inventory.Item item = OtherInventory.GetItem(itemID);
                                    Schema.Item schemaItem = CurrentSchema.GetItem(item.Defindex);
                                    OnUserRemoveItem(schemaItem, item);
                                }
                                break;
                            case TradeEventType.UserSetReady:
                                if (!isBot)
                                {
                                    otherIsReady = true;
                                    OnUserSetReady(true);
                                }
                                break;
                            case TradeEventType.UserSetUnReady:
                                if (!isBot)
                                {
                                    otherIsReady = false;
                                    OnUserSetReady(false);
                                }
                                break;
                            case TradeEventType.UserAccept:
                                if (!isBot)
                                {
                                    OnUserAccept();
                                }
                                break;
                            case TradeEventType.UserChat:
                                if (!isBot)
                                {
                                    OnMessage(trdEvent.text);
                                }
                                break;
                            default:
                                // Todo: add an OnWarning or similar event
                                if (OnError != null)
                                    OnError("Unknown Event ID: " + trdEvent.action);
                                break;
                        }

                        if (!isBot)
                            otherDidSomething = true;
                    }//if (!eventList.Contains(trdEvent))
                }// foreach (TradeEvent trdEvent in status.events)
            }//if (status.events != null)

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

            return otherDidSomething;
        }

        internal void FireOnCloseEvent()
        {
            var onCloseEvent = OnClose;

            if (onCloseEvent != null)
                onCloseEvent();
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

        void ValidateSteamItemChanged (ulong itemid, bool wasAdded)
        {
            // checks to make sure that the Trade polling saw
            // the correct change for the given item.

            // check if the correct item was added
            if (wasAdded && !myOfferedItems.ContainsValue (itemid))
                throw new TradeException ("Steam Trade had an invalid item added: " + itemid);

            // check if the correct item was removed
            if (!wasAdded && myOfferedItems.ContainsValue (itemid))
                throw new TradeException ("Steam Trade had an invalid item removed: " + itemid);
        }

        void ValidateLocalTradeItems ()
        {
            if (myOfferedItems.Count != steamMyOfferedItems.Count)
            {
                throw new TradeException ("Error validating local copy of items in the trade: Count mismatch");
            }

            foreach (ulong id in myOfferedItems.Values)
            {
                if (!steamMyOfferedItems.Contains (id))
                    throw new TradeException ("Error validating local copy of items in the trade: Item was not in the Steam Copy.");
            }
        }
    }
}

