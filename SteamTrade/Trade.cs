using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamTrade.Exceptions;
using SteamTrade.TradeWebAPI;

namespace SteamTrade
{
    /// <summary>
    /// Class which represents a trade.
    /// Note that the logic that Steam uses can be seen from their web-client source-code:  http://steamcommunity-a.akamaihd.net/public/javascript/economy_trade.js
    /// </summary>
    public partial class Trade
    {
        #region Static Public data

        public static Schema CurrentSchema = null;

        public enum TradeStatusType
        {
            OnGoing = 0,
            CompletedSuccessfully = 1,
            Empty = 2,
            TradeCancelled = 3,
            SessionExpired = 4,
            TradeFailed = 5,
            PendingConfirmation = 6
        }

        public string GetTradeStatusErrorString(TradeStatusType tradeStatusType)
        {
            switch(tradeStatusType)
            {
                case TradeStatusType.OnGoing:
                    return "is still going on";
                case TradeStatusType.CompletedSuccessfully:
                    return "completed successfully";
                case TradeStatusType.Empty:
                    return "completed empty - no items were exchanged";
                case TradeStatusType.TradeCancelled:
                    return "was cancelled " + (tradeCancelledByBot ? "by bot" : "by other user");
                case TradeStatusType.SessionExpired:
                    return String.Format("expired because {0} timed out", (otherUserTimingOut ? "other user" : "bot"));
                case TradeStatusType.TradeFailed:
                    return "failed unexpectedly";
                case TradeStatusType.PendingConfirmation:
                    return "completed - pending confirmation";
                default:
                    return "STATUS IS UNKNOWN - THIS SHOULD NEVER HAPPEN!";
            }
        }

        #endregion

        private const int WEB_REQUEST_MAX_RETRIES = 3;
        private const int WEB_REQUEST_TIME_BETWEEN_RETRIES_MS = 600;

        // list to store all trade events already processed
        private readonly List<TradeEvent> eventList;

        // current bot's sid
        private readonly SteamID mySteamId;

        private readonly Dictionary<int, TradeUserAssets> myOfferedItemsLocalCopy;
        private readonly TradeSession session;
        private readonly Task<Inventory> myInventoryTask;
        private readonly Task<Inventory> otherInventoryTask;
        private List<TradeUserAssets> myOfferedItems;
        private List<TradeUserAssets> otherOfferedItems;
        private bool otherUserTimingOut;
        private bool tradeCancelledByBot;
        private int numUnknownStatusUpdates;
        private long tradeOfferID; //Used for email confirmation

        internal Trade(SteamID me, SteamID other, SteamWeb steamWeb, Task<Inventory> myInventoryTask, Task<Inventory> otherInventoryTask)
        {
            TradeStarted = false;
            OtherIsReady = false;
            MeIsReady = false;
            mySteamId = me;
            OtherSID = other;

            session = new TradeSession(other, steamWeb);

            this.eventList = new List<TradeEvent>();

            myOfferedItemsLocalCopy = new Dictionary<int, TradeUserAssets>();
            otherOfferedItems = new List<TradeUserAssets>();
            myOfferedItems = new List<TradeUserAssets>();

            this.otherInventoryTask = otherInventoryTask;
            this.myInventoryTask = myInventoryTask;
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
        public Inventory OtherInventory
        {
            get
            {
                if(otherInventoryTask == null)
                    return null;

                otherInventoryTask.Wait();
                return otherInventoryTask.Result;
            }
        }

        /// <summary> 
        /// Gets the private inventory of the other user. 
        /// </summary>
        public ForeignInventory OtherPrivateInventory { get; private set; }

        /// <summary> 
        /// Gets the inventory of the bot.
        /// </summary>
        public Inventory MyInventory
        {
            get
            {
                if(myInventoryTask == null)
                    return null;

                myInventoryTask.Wait();
                return myInventoryTask.Result;
            }
        }

        /// <summary>
        /// Gets the items the user has offered, by itemid.
        /// </summary>
        /// <value>
        /// The other offered items.
        /// </value>
        public IEnumerable<TradeUserAssets> OtherOfferedItems
        {
            get { return otherOfferedItems; }
        }

        /// <summary>
        /// Gets the items the bot has offered, by itemid.
        /// </summary>
        /// <value>
        /// The bot offered items.
        /// </value>
        public IEnumerable<TradeUserAssets> MyOfferedItems
        {
            get { return myOfferedItems; }
        }

        /// <summary>
        /// Gets a value indicating if the other user is ready to trade.
        /// </summary>
        public bool OtherIsReady { get; private set; }

        /// <summary>
        /// Gets a value indicating if the bot is ready to trade.
        /// </summary>
        public bool MeIsReady { get; private set; }

        /// <summary>
        /// Gets a value indicating if a trade has started.
        /// </summary>
        public bool TradeStarted { get; private set; }

        /// <summary>
        /// Gets a value indicating if the remote trading partner cancelled the trade.
        /// </summary>
        public bool OtherUserCancelled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the trade completed normally. This
        /// is independent of other flags.
        /// </summary>
        public bool HasTradeCompletedOk { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the trade completed awaiting email confirmation. This
        /// is independent of other flags.
        /// </summary>
        public bool IsTradeAwaitingConfirmation { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the trade has finished (regardless of the cause, eg. success, cancellation, error, etc)
        /// </summary>
        public bool HasTradeEnded
        {
            get { return OtherUserCancelled || HasTradeCompletedOk || IsTradeAwaitingConfirmation || tradeCancelledByBot; }
        }

        /// <summary>
        /// Gets a value indicating if the remote trading partner accepted the trade.
        /// </summary>
        public bool OtherUserAccepted { get; private set; }

        #endregion

        #region Public Events

        public delegate void CloseHandler();

        public delegate void CompleteHandler();

        public delegate void WaitingForEmailHandler(long tradeOfferID);

        public delegate void ErrorHandler(string errorMessage);

        public delegate void StatusErrorHandler(TradeStatusType statusType);

        public delegate void TimeoutHandler();

        public delegate void SuccessfulInit();

        public delegate void UserAddItemHandler(Schema.Item schemaItem, Inventory.Item inventoryItem);

        public delegate void UserRemoveItemHandler(Schema.Item schemaItem, Inventory.Item inventoryItem);

        public delegate void MessageHandler(string msg);

        public delegate void UserSetReadyStateHandler(bool ready);

        public delegate void UserAcceptHandler();

        /// <summary>
        /// When the trade closes, this is called.  It doesn't matter
        /// whether or not it was a timeout or an error, this is called
        /// to close the trade.
        /// </summary>
        public event CloseHandler OnClose;

        /// <summary>
        /// Called when the trade completes successfully.
        /// </summary>
        public event CompleteHandler OnSuccess;

        /// <summary>
        /// Called when the trade ends awaiting email confirmation
        /// </summary>
        public event WaitingForEmailHandler OnAwaitingConfirmation;

        /// <summary>
        /// This is for handling errors that may occur, like inventories
        /// not loading.
        /// </summary>
        public event ErrorHandler OnError;

        /// <summary>
        /// Specifically for trade_status errors.
        /// </summary>
        public event StatusErrorHandler OnStatusError;

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
        public bool CancelTrade()
        {
            tradeCancelledByBot = true;
            return RetryWebRequest(session.CancelTradeWebCmd);
        }

        /// <summary>
        /// Adds a specified TF2 item by its itemid.
        /// If the item is not a TF2 item, use the AddItem(ulong itemid, int appid, long contextid) overload
        /// </summary>
        /// <returns><c>false</c> if the tf2 item was not found in the inventory.</returns>
        public bool AddItem(ulong itemid)
        {
            if(MyInventory.GetItem(itemid) == null)
            {
                return false;
            }
            else
            {
                return AddItem(new TradeUserAssets(440, 2, itemid));
            }
        }

        public bool AddItem(ulong itemid, int appid, long contextid)
        {
            return AddItem(new TradeUserAssets(appid, contextid, itemid));
        }

        public bool AddItem(TradeUserAssets item)
        {
            var slot = NextTradeSlot();
            bool success = RetryWebRequest(() => session.AddItemWebCmd(item.assetid, slot, item.appid, item.contextid));

            if(success)
                myOfferedItemsLocalCopy[slot] = item;

            return success;
        }

        /// <summary>
        /// Adds a single item by its Defindex.
        /// </summary>
        /// <returns>
        /// <c>true</c> if an item was found with the corresponding
        /// defindex, <c>false</c> otherwise.
        /// </returns>
        public bool AddItemByDefindex(int defindex)
        {
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex(defindex);
            foreach(Inventory.Item item in items)
            {
                if(item != null && myOfferedItemsLocalCopy.Values.All(o => o.assetid != item.Id) && !item.IsNotTradeable)
                {
                    return AddItem(item.Id);
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
        public uint AddAllItemsByDefindex(int defindex, uint numToAdd = 0)
        {
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex(defindex);

            uint added = 0;

            foreach(Inventory.Item item in items)
            {
                if(item != null && myOfferedItemsLocalCopy.Values.All(o => o.assetid != item.Id) && !item.IsNotTradeable)
                {
                    bool success = AddItem(item.Id);

                    if(success)
                        added++;

                    if(numToAdd > 0 && added >= numToAdd)
                        return added;
                }
            }

            return added;
        }


        public bool RemoveItem(TradeUserAssets item)
        {
            return RemoveItem(item.assetid, item.appid, item.contextid);
        }

        /// <summary>
        /// Removes an item by its itemid.
        /// </summary>
        /// <returns><c>false</c> the item was not found in the trade.</returns>
        public bool RemoveItem(ulong itemid, int appid = 440, long contextid = 2)
        {
            int? slot = GetItemSlot(itemid);
            if(!slot.HasValue)
                return false;

            bool success = RetryWebRequest(() => session.RemoveItemWebCmd(itemid, slot.Value, appid, contextid));

            if(success)
                myOfferedItemsLocalCopy.Remove(slot.Value);

            return success;
        }

        /// <summary>
        /// Removes an item with the given Defindex from the trade.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if it found a corresponding item; <c>false</c> otherwise.
        /// </returns>
        public bool RemoveItemByDefindex(int defindex)
        {
            foreach(TradeUserAssets asset in myOfferedItemsLocalCopy.Values)
            {
                Inventory.Item item = MyInventory.GetItem(asset.assetid);
                if(item != null && item.Defindex == defindex)
                {
                    return RemoveItem(item.Id);
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
        public uint RemoveAllItemsByDefindex(int defindex, uint numToRemove = 0)
        {
            List<Inventory.Item> items = MyInventory.GetItemsByDefindex(defindex);

            uint removed = 0;

            foreach(Inventory.Item item in items)
            {
                if(item != null && myOfferedItemsLocalCopy.Values.Any(o => o.assetid == item.Id))
                {
                    bool success = RemoveItem(item.Id);

                    if(success)
                        removed++;

                    if(numToRemove > 0 && removed >= numToRemove)
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
            uint numRemoved = 0;

            foreach(TradeUserAssets asset in myOfferedItemsLocalCopy.Values.ToList())
            {
                Inventory.Item item = MyInventory.GetItem(asset.assetid);

                if(item != null)
                {
                    bool wasRemoved = RemoveItem(item.Id);

                    if(wasRemoved)
                        numRemoved++;
                }
            }

            return numRemoved;
        }

        /// <summary>
        /// Sends a message to the user over the trade chat.
        /// </summary>
        public bool SendMessage(string msg)
        {
            return RetryWebRequest(() => session.SendMessageWebCmd(msg));
        }

        /// <summary>
        /// Sets the bot to a ready status.
        /// </summary>
        public bool SetReady(bool ready)
        {
            //If the bot calls SetReady(false) and the call fails, we still want meIsReady to be
            //set to false.  Otherwise, if the call to SetReady() was a result of a callback
            //from Trade.Poll() inside of the OnTradeAccept() handler, the OnTradeAccept()
            //handler might think the bot is ready, when really it's not!
            if(!ready)
                MeIsReady = false;

            ValidateLocalTradeItems();

            return RetryWebRequest(() => session.SetReadyWebCmd(ready));
        }

        /// <summary>
        /// Accepts the trade from the user.  Returns whether the acceptance went through or not
        /// </summary>
        public bool AcceptTrade()
        {
            if(!MeIsReady)
                return false;

            ValidateLocalTradeItems();

            return RetryWebRequest(session.AcceptTradeWebCmd);
        }

        /// <summary>
        /// Calls the given function multiple times, until we get a non-null/non-false/non-zero result, or we've made at least
        /// WEB_REQUEST_MAX_RETRIES attempts (with WEB_REQUEST_TIME_BETWEEN_RETRIES_MS between attempts)
        /// </summary>
        /// <returns>The result of the function if it succeeded, or default(T) (null/false/0) otherwise</returns>
        private T RetryWebRequest<T>(Func<T> webEvent)
        {
            for(int i = 0; i < WEB_REQUEST_MAX_RETRIES; i++)
            {
                //Don't make any more requests if the trade has ended!
                if (HasTradeEnded)
                    return default(T);

                try
                {
                    T result = webEvent();

                    // if the web request returned some error.
                    if(!EqualityComparer<T>.Default.Equals(result, default(T)))
                        return result;
                }
                catch(Exception ex)
                {
                    // TODO: log to SteamBot.Log but... see issue #394
                    // realistically we should not throw anymore
                    Console.WriteLine(ex);
                }

                if(i != WEB_REQUEST_MAX_RETRIES)
                {
                    //This will cause the bot to stop responding while we wait between web requests.  ...Is this really what we want?
                    Thread.Sleep(WEB_REQUEST_TIME_BETWEEN_RETRIES_MS);
                }
            }

            return default(T);
        }

        /// <summary>
        /// This updates the trade.  This is called at an interval of a
        /// default of 800ms, not including the execution time of the
        /// method itself.
        /// </summary>
        /// <returns><c>true</c> if the other trade partner performed an action; otherwise <c>false</c>.</returns>
        public bool Poll()
        {
            if(!TradeStarted)
            {
                TradeStarted = true;

                // since there is no feedback to let us know that the trade
                // is fully initialized we assume that it is when we start polling.
                if(OnAfterInit != null)
                    OnAfterInit();
            }

            TradeStatus status = RetryWebRequest(session.GetStatus);

            if(status == null)
                return false;

            TradeStatusType tradeStatusType = (TradeStatusType) status.trade_status;
            switch (tradeStatusType)
            {
                // Nothing happened. i.e. trade hasn't closed yet.
                case TradeStatusType.OnGoing:
                    return HandleTradeOngoing(status);

                // Successful trade
                case TradeStatusType.CompletedSuccessfully:
                    HasTradeCompletedOk = true;
                    return false;

                // Email/mobile confirmation
                case TradeStatusType.PendingConfirmation:
                    IsTradeAwaitingConfirmation = true;
                    tradeOfferID = long.Parse(status.tradeid);
                    return false;

                //On a status of 2, the Steam web code attempts the request two more times
                case TradeStatusType.Empty:
                    numUnknownStatusUpdates++;
                    if(numUnknownStatusUpdates < 3)
                    {
                        return false;
                    }
                    break;
            }

            FireOnStatusErrorEvent(tradeStatusType);
            OtherUserCancelled = true;
            return false;
        }

        private bool HandleTradeOngoing(TradeStatus status)
        {
            bool otherUserDidSomething = false;
            if (status.newversion)
            {
                HandleTradeVersionChange(status);
                otherUserDidSomething = true;
            }
            else if(status.version > session.Version)
            {
                // oh crap! we missed a version update abort so we don't get 
                // scammed. if we could get what steam thinks what's in the 
                // trade then this wouldn't be an issue. but we can only get 
                // that when we see newversion == true
                throw new TradeException("The trade version does not match. Aborting.");
            }

            // Update Local Variables
            if(status.them != null)
            {
                OtherIsReady = status.them.ready == 1;
                MeIsReady = status.me.ready == 1;
                OtherUserAccepted = status.them.confirmed == 1;

                //Similar to the logic Steam uses to determine whether or not to show the "waiting" spinner in the trade window
                otherUserTimingOut = (status.them.connection_pending || status.them.sec_since_touch >= 5);
            }

            var events = status.GetAllEvents();
            foreach(var tradeEvent in events.OrderBy(o => o.timestamp))
            {
                if(eventList.Contains(tradeEvent))
                    continue;

                //add event to processed list, as we are taking care of this event now
                eventList.Add(tradeEvent);

                bool isBot = tradeEvent.steamid == MySteamId.ConvertToUInt64().ToString();

                // dont process if this is something the bot did
                if(isBot)
                    continue;

                otherUserDidSomething = true;
                switch((TradeEventType) tradeEvent.action)
                {
                    case TradeEventType.ItemAdded:
                        TradeUserAssets newAsset = new TradeUserAssets(tradeEvent.appid, tradeEvent.contextid, tradeEvent.assetid);
                        if(!otherOfferedItems.Contains(newAsset))
                        {
                            otherOfferedItems.Add(newAsset);
                            FireOnUserAddItem(newAsset);
                        }
                        break;
                    case TradeEventType.ItemRemoved:
                        TradeUserAssets oldAsset = new TradeUserAssets(tradeEvent.appid, tradeEvent.contextid, tradeEvent.assetid);
                        if(otherOfferedItems.Contains(oldAsset))
                        {
                            otherOfferedItems.Remove(oldAsset);
                            FireOnUserRemoveItem(oldAsset);
                        }
                        break;
                    case TradeEventType.UserSetReady:
                        OnUserSetReady(true);
                        break;
                    case TradeEventType.UserSetUnReady:
                        OnUserSetReady(false);
                        break;
                    case TradeEventType.UserAccept:
                        OnUserAccept();
                        break;
                    case TradeEventType.UserChat:
                        OnMessage(tradeEvent.text);
                        break;
                    default:
                        throw new TradeException("Unknown event type: " + tradeEvent.action);
                }
            }

            if(status.logpos != 0)
            {
                session.LogPos = status.logpos;
            }

            return otherUserDidSomething;
        }

        private void HandleTradeVersionChange(TradeStatus status)
        {
            //Figure out which items have been added/removed
            IEnumerable<TradeUserAssets> otherOfferedItemsUpdated = status.them.GetAssets();
            IEnumerable<TradeUserAssets> addedItems = otherOfferedItemsUpdated.Except(otherOfferedItems).ToList();
            IEnumerable<TradeUserAssets> removedItems = otherOfferedItems.Except(otherOfferedItemsUpdated).ToList();

            //Copy over the new items and update the version number
            otherOfferedItems = status.them.GetAssets().ToList();
            myOfferedItems = status.me.GetAssets().ToList();
            session.Version = status.version;

            //Fire the OnUserRemoveItem events
            foreach (TradeUserAssets asset in removedItems)
            {
                FireOnUserRemoveItem(asset);
            }

            //Fire the OnUserAddItem events
            foreach (TradeUserAssets asset in addedItems)
            {
                FireOnUserAddItem(asset);
            }
        }

        /// <summary>
        /// Gets an item from a TradeEvent, and passes it into the UserHandler's implemented OnUserAddItem([...]) routine.
        /// Passes in null items if something went wrong.
        /// </summary>
        private void FireOnUserAddItem(TradeUserAssets asset)
        {
            if(MeIsReady)
            {
                SetReady(false);
            }

            if(OtherInventory != null && !OtherInventory.IsPrivate)
            {
                Inventory.Item item = OtherInventory.GetItem(asset.assetid);
                if(item != null)
                {
                    Schema.Item schemaItem = CurrentSchema.GetItem(item.Defindex);
                    if(schemaItem == null)
                    {
                        Console.WriteLine("User added an unknown item to the trade.");
                    }

                    OnUserAddItem(schemaItem, item);
                }
                else
                {
                    item = new Inventory.Item
                    {
                        Id = asset.assetid,
                        AppId = asset.appid,
                        ContextId = asset.contextid
                    };
                    //Console.WriteLine("User added a non TF2 item to the trade.");
                    OnUserAddItem(null, item);
                }
            }
            else
            {
                var schemaItem = GetItemFromPrivateBp(asset);
                if(schemaItem == null)
                {
                    Console.WriteLine("User added an unknown item to the trade.");
                }

                OnUserAddItem(schemaItem, null);
                // todo: figure out what to send in with Inventory item.....
            }
        }

        private Schema.Item GetItemFromPrivateBp(TradeUserAssets asset)
        {
            if (OtherPrivateInventory == null)
            {
                dynamic foreignInventory = session.GetForeignInventory(OtherSID, asset.contextid, asset.appid);
                if (foreignInventory == null || foreignInventory.success == null || !foreignInventory.success.Value)
                {
                    return null;
                }

                OtherPrivateInventory = new ForeignInventory(foreignInventory);
            }

            int defindex = OtherPrivateInventory.GetDefIndex(asset.assetid);
            Schema.Item schemaItem = CurrentSchema.GetItem(defindex);
            return schemaItem;
        }

        /// <summary>
        /// Gets an item from a TradeEvent, and passes it into the UserHandler's implemented OnUserRemoveItem([...]) routine.
        /// Passes in null items if something went wrong.
        /// </summary>
        /// <returns></returns>
        private void FireOnUserRemoveItem(TradeUserAssets asset)
        {
            if(MeIsReady)
            {
                SetReady(false);
            }

            if(OtherInventory != null)
            {
                Inventory.Item item = OtherInventory.GetItem(asset.assetid);
                if(item != null)
                {
                    Schema.Item schemaItem = CurrentSchema.GetItem(item.Defindex);
                    if(schemaItem == null)
                    {
                        // TODO: Add log (counldn't find item in CurrentSchema)
                    }

                    OnUserRemoveItem(schemaItem, item);
                }
                else
                {
                    // TODO: Log this (Couldn't find item in user's inventory can't find item in CurrentSchema
                    item = new Inventory.Item
                    {
                        Id = asset.assetid,
                        AppId = asset.appid,
                        ContextId = asset.contextid
                    };
                    OnUserRemoveItem(null, item);
                }
            }
            else
            {
                var schemaItem = GetItemFromPrivateBp(asset);
                if(schemaItem == null)
                {
                    // TODO: Add log (counldn't find item in CurrentSchema)
                }

                OnUserRemoveItem(schemaItem, null);
            }
        }

        internal void FireOnSuccessEvent()
        {
            var onSuccessEvent = OnSuccess;

            if(onSuccessEvent != null)
                onSuccessEvent();
        }

        internal void FireOnAwaitingConfirmation()
        {
            var onAwaitingConfirmation = OnAwaitingConfirmation;

            if (onAwaitingConfirmation != null)
                onAwaitingConfirmation(tradeOfferID);
        }

        internal void FireOnCloseEvent()
        {
            var onCloseEvent = OnClose;

            if(onCloseEvent != null)
                onCloseEvent();
        }

        internal void FireOnErrorEvent(string message)
        {
            var onErrorEvent = OnError;

            if(onErrorEvent != null)
                onErrorEvent(message);
        }

        internal void FireOnStatusErrorEvent(TradeStatusType statusType)
        {
            var onStatusErrorEvent = OnStatusError;

            if (onStatusErrorEvent != null)
                onStatusErrorEvent(statusType);
        }

        private int NextTradeSlot()
        {
            int slot = 0;
            while(myOfferedItemsLocalCopy.ContainsKey(slot))
            {
                slot++;
            }
            return slot;
        }

        private int? GetItemSlot(ulong itemid)
        {
            foreach(int slot in myOfferedItemsLocalCopy.Keys)
            {
                if(myOfferedItemsLocalCopy[slot].assetid == itemid)
                {
                    return slot;
                }
            }
            return null;
        }

        private void ValidateLocalTradeItems()
        {
            if (!myOfferedItemsLocalCopy.Values.OrderBy(o => o).SequenceEqual(MyOfferedItems.OrderBy(o => o)))
            {
                throw new TradeException("Error validating local copy of offered items in the trade");
            }
        }
    }
}
