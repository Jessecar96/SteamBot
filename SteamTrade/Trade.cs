using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamTrade.Exceptions;
using SteamTrade.TradeWebAPI;

namespace SteamTrade
{
    public partial class Trade
    {
        #region Static Public data
        public static TF2Schema CurrentSchema = null;
        #endregion

        private const int WEB_REQUEST_MAX_RETRIES = 3;
        private const int WEB_REQUEST_TIME_BETWEEN_RETRIES_MS = 600;

        // list to store all trade events already processed
        private readonly List<TradeEvent> eventList;

        // current bot's sid
        private readonly SteamID mySteamId;

        private readonly Dictionary<int, GenericInventory.GenericItem> steamMyOfferedItems;
        private readonly TradeSession session;                

        internal Trade(SteamID me, SteamID other, string sessionId, string token, GenericInventory myInventory, GenericInventory otherInventory)
        {
            TradeStarted = false;
            OtherIsReady = false;
            MeIsReady = false;
            mySteamId = me;
            OtherSID = other;

            session = new TradeSession(sessionId, token, other);

            this.eventList = new List<TradeEvent>();

            OtherOfferedItems = new List<GenericInventory.GenericItem>();
            MyOfferedItems = new List<GenericInventory.GenericItem>();
            steamMyOfferedItems = new Dictionary<int, GenericInventory.GenericItem>();

            MyInventory = myInventory;
            OtherInventory = otherInventory;
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
        /// Gets the inventory of the bot
        /// </summary>
        public readonly GenericInventory MyInventory;

        /// <summary>
        /// Gets the inventory of the other user
        /// </summary>
        public readonly GenericInventory OtherInventory;

        /// <summary> 
        /// Gets the private inventory of the other user. 
        /// </summary>
        public ForeignInventory OtherPrivateInventory { get; private set; }        

        /// <summary>
        /// Gets the items the user has offered
        /// </summary>
        /// <value>
        /// The other offered items.
        /// </value>
        public List<GenericInventory.GenericItem> OtherOfferedItems { get; private set; }

        /// <summary>
        /// Gets the items the bot has offered
        /// </summary>
        /// <value>
        /// The bot offered items.
        /// </value>
        public List<GenericInventory.GenericItem> MyOfferedItems { get; private set; }

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
        /// Gets a value indicating if the remote trading partner accepted the trade.
        /// </summary>
        public bool OtherUserAccepted { get; private set; }

        #endregion

        #region Public Events

        public delegate void CloseHandler();

        public delegate void CompleteHandler();

        public delegate void ErrorHandler(string error);

        public delegate void TimeoutHandler();

        public delegate void SuccessfulInit();

        public delegate void UserAddItemHandler(GenericInventory.Inventory.Item inventoryItem);

        public delegate void UserRemoveItemHandler(GenericInventory.Inventory.Item inventoryItem);

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
        public bool CancelTrade()
        {
            return RetryWebRequest(session.CancelTradeWebCmd);
        }

        public bool AddItem(ulong itemid, int appid, long contextid, int amount)
        {
            return AddItem(new TradeUserAssets() { assetid = itemid, appid = appid, contextid = contextid, amount = amount });
        }
        public bool AddItem(TradeUserAssets item)
        {
            var slot = NextTradeSlot();
            bool success = RetryWebRequest(() => session.AddItemWebCmd(item.assetid, slot, item.appid, item.contextid, item.amount));

            if (success)
                steamMyOfferedItems[slot] = new GenericInventory.GenericItem(item.appid, item.contextid, item.assetid, item.amount);

            return success;
        }

        public bool RemoveItem(TradeUserAssets item)
        {
            return RemoveItem(item.assetid, item.appid, item.contextid, item.amount);
        }

        /// <summary>
        /// Removes an item by its itemid.
        /// </summary>
        /// <returns><c>false</c> the item was not found in the trade.</returns>
        public bool RemoveItem(ulong itemid, int appid, long contextid, int amount)
        {
            int? slot = GetItemSlot(itemid, appid, contextid, amount);
            if (!slot.HasValue)
                return false;

            bool success = RetryWebRequest(() => session.RemoveItemWebCmd(itemid, slot.Value, appid, contextid, amount));

            if (success)
                steamMyOfferedItems.Remove(slot.Value);

            return success;
        }

        /// <summary>
        /// Removes all offered items from the trade.
        /// </summary>
        /// <returns>Number of items removed.</returns>
        public uint RemoveAllItems()
        {
            uint numRemoved = 0;

            foreach (var item in steamMyOfferedItems.Values.ToList())
            {
                bool wasRemoved = RemoveItem(item.ItemId, item.AppId, item.ContextId, item.Amount);

                if (wasRemoved)
                    numRemoved++;
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
            if (!ready)
                MeIsReady = false;

            // testing
            ValidateLocalTradeItems();

            return RetryWebRequest(() => session.SetReadyWebCmd(ready));
        }

        /// <summary>
        /// Accepts the trade from the user.  Returns a deserialized
        /// JSON object.
        /// </summary>
        public bool AcceptTrade()
        {
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
            for (int i = 0; i < WEB_REQUEST_MAX_RETRIES; i++)
            {
                //Don't make any more requests if the trade has ended!
                if (HasTradeCompletedOk || OtherUserCancelled)
                    return default(T);

                try
                {
                    T result = webEvent();

                    // if the web request returned some error.
                    if (!EqualityComparer<T>.Default.Equals(result, default(T)))
                        return result;
                }
                catch (Exception ex)
                {
                    // TODO: log to SteamBot.Log but... see issue #394
                    // realistically we should not throw anymore
                    Console.WriteLine(ex);
                }

                if (i != WEB_REQUEST_MAX_RETRIES)
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
            bool otherDidSomething = false;

            if (!TradeStarted)
            {
                TradeStarted = true;

                // since there is no feedback to let us know that the trade
                // is fully initialized we assume that it is when we start polling.
                if (OnAfterInit != null)
                    OnAfterInit();
            }

            TradeStatus status = RetryWebRequest(session.GetStatus);

            if (status == null)
                return false;

            switch (status.trade_status)
            {
                // Nothing happened. i.e. trade hasn't closed yet.
                case 0:
                    break;

                // Successful trade
                case 1:
                    HasTradeCompletedOk = true;                    
                    break;

                // All other known values (3, 4) correspond to trades closing.
                default:
                    EnqueueAction(() => FireOnErrorEvent("Trade was closed by other user. Trade status: " + status.trade_status));
                    OtherUserCancelled = true;
                    break;
            }

            if (status.newversion)
            {
                // handle item adding and removing
                session.Version = status.version;

                HandleTradeVersionChange(status);
                return true;
            }
            else if (status.version > session.Version)
            {
                // oh crap! we missed a version update abort so we don't get 
                // scammed. if we could get what steam thinks what's in the 
                // trade then this wouldn't be an issue. but we can only get 
                // that when we see newversion == true
                throw new TradeException("The trade version does not match. Aborting.");
            }

            // Update Local Variables
            if (status.them != null)
            {
                OtherIsReady = status.them.ready == 1;
                MeIsReady = status.me.ready == 1;
                OtherUserAccepted = status.them.confirmed == 1;
            }

            var events = status.GetAllEvents();
            foreach (var tradeEvent in events)
            {
                if (eventList.Contains(tradeEvent))
                    continue;

                //add event to processed list, as we are taking care of this event now
                eventList.Add(tradeEvent);

                bool isBot = tradeEvent.steamid == MySteamId.ConvertToUInt64().ToString();

                // dont process if this is something the bot did
                if (isBot)
                    continue;

                otherDidSomething = true;

                switch ((TradeEventType)tradeEvent.action)
                {
                    case TradeEventType.ItemAdded:
                        EnqueueAction(() => FireOnUserAddItem(tradeEvent));
                        break;
                    case TradeEventType.ItemRemoved:
                        EnqueueAction(() => FireOnUserRemoveItem(tradeEvent));
                        break;
                    case TradeEventType.UserSetReady:
                        EnqueueAction(() => OnUserSetReady(true));
                        break;
                    case TradeEventType.UserSetUnReady:
                        EnqueueAction(() => OnUserSetReady(false));
                        break;
                    case TradeEventType.UserAccept:
                        EnqueueAction(() => OnUserAccept());
                        break;
                    case TradeEventType.UserChat:
                        EnqueueAction(() => OnMessage(tradeEvent.text));
                        break;
                    default:
                        // Todo: add an OnWarning or similar event
                        EnqueueAction(() => FireOnErrorEvent("Unknown Event ID: " + tradeEvent.action));
                        break;
                }
            }

            if (status.logpos != 0)
            {
                session.LogPos = status.logpos;
            }

            return otherDidSomething;
        }

        private object queueLock = new object();
        private Task latestTask;

        public void EnqueueAction(System.Action action)
        {
            lock (queueLock)
            {
                if (latestTask == null)
                    latestTask = Task.Factory.StartNew(action);
                else
                    latestTask = latestTask.ContinueWith(tsk => action());
            }
        }

        private void HandleTradeVersionChange(TradeStatus status)
        {
            CopyNewAssets(OtherOfferedItems, status.them.GetAssets());
            CopyNewAssets(MyOfferedItems, status.me.GetAssets());
        }

        private void CopyNewAssets(List<GenericInventory.GenericItem> dest, IEnumerable<TradeUserAssets> assetList)
        {
            if (assetList == null)
                return;

            dest.Clear();
            foreach (var asset in assetList)
            {
                var genericItem = new GenericInventory.GenericItem(asset.appid, asset.contextid, asset.assetid, asset.amount);
                dest.Add(genericItem);
            }
        }

        /// <summary>
        /// Gets an item from a TradeEvent, and passes it into the UserHandler's implemented OnUserAddItem([...]) routine.
        /// Passes in null items if something went wrong.
        /// </summary>
        /// <param name="tradeEvent">TradeEvent to get item from</param>
        /// <returns></returns>
        private void FireOnUserAddItem(TradeEvent tradeEvent)
        {
            var item = OtherInventory.GetItem(tradeEvent.appid, tradeEvent.contextid, tradeEvent.assetid);
            OnUserAddItem(item);
        }

        /// <summary>
        /// Gets an item from a TradeEvent, and passes it into the UserHandler's implemented OnUserRemoveItem([...]) routine.
        /// Passes in null items if something went wrong.
        /// </summary>
        /// <param name="tradeEvent">TradeEvent to get item from</param>
        /// <returns></returns>
        private void FireOnUserRemoveItem(TradeEvent tradeEvent)
        {
            var item = OtherInventory.GetItem(tradeEvent.appid, tradeEvent.contextid, tradeEvent.assetid);
            OnUserRemoveItem(item);
        }

        internal void FireOnSuccessEvent()
        {
            var onSuccessEvent = OnSuccess;

            if (onSuccessEvent != null)
                onSuccessEvent();
        }

        internal void FireOnCloseEvent()
        {
            var onCloseEvent = OnClose;

            if (onCloseEvent != null)
                onCloseEvent();
        }

        internal void FireOnErrorEvent(string errorMessage)
        {
            var onErrorEvent = OnError;

            if (onErrorEvent != null)
                onErrorEvent(errorMessage);
        }

        private int NextTradeSlot()
        {
            int slot = 0;
            while (steamMyOfferedItems.ContainsKey(slot))
            {
                slot++;
            }
            return slot;
        }

        private int? GetItemSlot(ulong itemid, int appid, long contextid, int amount)
        {
            var item = new GenericInventory.GenericItem(appid, contextid, itemid, amount);
            foreach (int slot in steamMyOfferedItems.Keys)
            {
                if (steamMyOfferedItems[slot].Equals(item))
                {
                    return slot;
                }
            }
            return null;
        }

        private void ValidateLocalTradeItems()
        {
            if (steamMyOfferedItems.Count != MyOfferedItems.Count)
            {
                throw new TradeException("Error validating local copy of items in the trade: Count mismatch");
            }

            if (steamMyOfferedItems.Values.Any(id => !MyOfferedItems.Contains(id)))
            {
                throw new TradeException("Error validating local copy of items in the trade: Item was not in the Steam Copy.");
            }
        }
    }
}
