using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using SteamKit2;
using System.Net;
using System.Web;
using System.IO;
using Newtonsoft.Json;

namespace SteamAPI
{
    public class TradeOffers
    {
        public SteamID BotId;
        private SteamWeb SteamWeb;
        public List<ulong> OurPendingTradeOffers;
        private List<ulong> ReceivedPendingTradeOffers;
        private string AccountApiKey;
        private bool ShouldCheckPendingTradeOffers;
        private int TradeOfferRefreshRate;

        public TradeOffers(SteamID botId, SteamWeb steamWeb, string accountApiKey, int TradeOfferRefreshRate, List<ulong> pendingTradeOffers = null)
        {
            this.BotId = botId;
            this.SteamWeb = steamWeb;
            this.AccountApiKey = accountApiKey;
            this.ShouldCheckPendingTradeOffers = true;
            this.TradeOfferRefreshRate = TradeOfferRefreshRate;

            if (pendingTradeOffers == null)
                this.OurPendingTradeOffers = new List<ulong>();
            else
                this.OurPendingTradeOffers = pendingTradeOffers;

            this.ReceivedPendingTradeOffers = new List<ulong>();

            new System.Threading.Thread(CheckPendingTradeOffers).Start();
        }        

        /// <summary>
        /// Create a new trade offer session.
        /// </summary>
        /// <param name="partnerId">The SteamID of the user you want to send a trade offer to.</param>
        /// <returns>A 'Trade' object in which you can apply further actions</returns>
        public Trade CreateTrade(SteamID partnerId)
        {
            return new Trade(this, partnerId, SteamWeb);
        }

        public class Trade
        {
            private TradeOffers TradeOffers;
            private CookieContainer cookies = new CookieContainer();
            private SteamWeb steamWeb;
            private SteamID partnerId;
            public TradeStatus tradeStatus;

            public Trade(TradeOffers tradeOffers, SteamID partnerId, SteamWeb steamWeb)
            {
                this.TradeOffers = tradeOffers;
                this.partnerId = partnerId;
                this.steamWeb = steamWeb;
                tradeStatus = new TradeStatus();
                tradeStatus.version = 1;
                tradeStatus.newversion = true;
                tradeStatus.me = new TradeStatusUser(ref tradeStatus);
                tradeStatus.them = new TradeStatusUser(ref tradeStatus);
            }

            /// <summary>
            /// Send the current trade offer with a token.
            /// </summary>
            /// <param name="message">Message to send with trade offer.</param>
            /// <param name="token">Trade offer token.</param>
            /// <returns>-1 if response fails to deserialize (general error), 0 if no tradeofferid exists (Steam error), or the Trade Offer ID of the newly created trade offer.</returns>
            public ulong SendTradeWithToken(string message, string token)
            {
                return SendTrade(message, token);
            }
            /// <summary>
            /// Send the current trade offer.
            /// </summary>
            /// <param name="message">Message to send with trade offer.</param>
            /// <param name="token">Optional trade offer token.</param>
            /// <returns>-1 if response fails to deserialize (general error), 0 if no tradeofferid exists (Steam error), or the Trade Offer ID of the newly created trade offer.</returns>
            public ulong SendTrade(string message, string token = "")
            {
                var url = "https://steamcommunity.com/tradeoffer/new/send";
                var referer = "https://steamcommunity.com/tradeoffer/new/?partner=" + partnerId.AccountID;
                var data = new NameValueCollection();
                data.Add("sessionid", steamWeb.SessionId);
                data.Add("serverid", "1");
                data.Add("partner", partnerId.ConvertToUInt64().ToString());
                data.Add("tradeoffermessage", message);                
                data.Add("json_tradeoffer", JsonConvert.SerializeObject(this.tradeStatus));
                data.Add("trade_offer_create_params", token == "" ? "{}" : "{\"trade_offer_access_token\":\"" + token + "\"}");
                try
                {
                    string response = RetryWebRequest(steamWeb, url, "POST", data, true, referer);
                    dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(response);
                    try
                    {
                        ulong tradeOfferId = Convert.ToUInt64(jsonResponse.tradeofferid);
                        this.TradeOffers.AddPendingTradeOfferToList(tradeOfferId);
                        return tradeOfferId;
                    }
                    catch
                    {
                        return 0;
                    }
                }
                catch
                {
                    return 0;
                }
            }

            /// <summary>
            /// Add a bot's item to the trade offer.
            /// </summary>
            /// <param name="asset">TradeAsset object</param>
            /// <returns>True if item hasn't been added already, false if it has.</returns>
            public bool AddMyItem(TradeAsset asset)
            {
                return tradeStatus.me.AddItem(asset);
            }
            /// <summary>
            /// Add a bot's item to the trade offer.
            /// </summary>
            /// <param name="appId">App ID of item</param>
            /// <param name="contextId">Context ID of item</param>
            /// <param name="assetId">Asset (unique) ID of item</param>
            /// <param name="amount">Amount to add (default = 1)</param>
            /// <returns>True if item hasn't been added already, false if it has.</returns>
            public bool AddMyItem(int appId, ulong contextId, ulong assetId, int amount = 1)
            {
                var asset = new TradeAsset(appId, contextId, assetId, amount);
                return tradeStatus.me.AddItem(asset);
            }

            /// <summary>
            /// Add a user's item to the trade offer.
            /// </summary>
            /// <param name="asset">TradeAsset object</param>
            /// <returns>True if item hasn't been added already, false if it has.</returns>
            public bool AddOtherItem(TradeAsset asset)
            {
                return tradeStatus.them.AddItem(asset);
            }
            /// <summary>
            /// Add a user's item to the trade offer.
            /// </summary>
            /// <param name="appId">App ID of item</param>
            /// <param name="contextId">Context ID of item</param>
            /// <param name="assetId">Asset (unique) ID of item</param>
            /// <param name="amount">Amount to add (default = 1)</param>
            /// <returns>True if item hasn't been added already, false if it has.</returns>
            public bool AddOtherItem(int appId, ulong contextId, ulong assetId, int amount = 1)
            {
                var asset = new TradeAsset(appId, contextId, assetId, amount);
                return tradeStatus.them.AddItem(asset);
            }

            public class TradeStatus
            {
                public bool newversion { get; set; }
                public int version { get; set; }
                public TradeStatusUser me { get; set; }
                public TradeStatusUser them { get; set; }
                [JsonIgnore]
                public string message { get; set; }
                [JsonIgnore]
                public ulong tradeid { get; set; }
            }

            public class TradeStatusUser
            {                
                public List<TradeAsset> assets { get; set; }
                public List<TradeAsset> currency = new List<TradeAsset>();
                public bool ready { get; set; }
                [JsonIgnore]
                public TradeStatus tradeStatus;
                [JsonIgnore]
                public SteamID steamId;

                public TradeStatusUser(ref TradeStatus tradeStatus)
                {
                    this.tradeStatus = tradeStatus;
                    ready = false;
                    assets = new List<TradeAsset>();
                }

                public bool AddItem(TradeAsset asset)
                {
                    if (!assets.Contains(asset))
                    {
                        tradeStatus.version++;
                        assets.Add(asset);
                        return true;
                    }
                    return false;
                }
                public bool AddItem(int appId, ulong contextId, ulong assetId, int amount = 1)
                {
                    var asset = new TradeAsset(appId, contextId, assetId, amount);
                    return AddItem(asset);
                }
            }

            public class TradeAsset
            {
                public int appid;
                public string contextid;
                public int amount;
                public string assetid;

                public TradeAsset(int appId, ulong contextId, ulong itemId, int amount)
                {
                    this.appid = appId;
                    this.contextid = contextId.ToString();
                    this.assetid = itemId.ToString();
                    this.amount = amount;
                }

                public TradeAsset(int appId, string contextId, string itemId, int amount)
                {
                    this.appid = appId;
                    this.contextid = contextId;
                    this.assetid = itemId;
                    this.amount = amount;
                }

                public override bool Equals(object obj)
                {
                    if (obj == null)
                        return false;

                    TradeAsset other = obj as TradeAsset;
                    if ((Object)other == null)
                        return false;

                    return Equals(other);
                }

                public bool Equals(TradeAsset other)
                {
                    return (this.appid == other.appid) &&
                            (this.contextid == other.contextid) &&
                            (this.amount == other.amount) &&
                            (this.assetid == other.assetid);
                }

                public override int GetHashCode()
                {
                    return (Convert.ToUInt64(appid) ^ Convert.ToUInt64(contextid) ^ Convert.ToUInt64(amount) ^ Convert.ToUInt64(assetid)).GetHashCode();
                }
            }
        }

        /// <summary>
        /// Accepts a pending trade offer.
        /// </summary>
        /// <param name="tradeOfferId">The ID of the trade offer</param>
        /// <returns>True if successful, false if not</returns>
        public bool AcceptTrade(ulong tradeOfferId)
        {
            GetTradeOfferAPI.GetTradeOfferResponse tradeOfferResponse = GetTradeOffer(tradeOfferId);
            bool response = AcceptTrade(tradeOfferResponse.Offer);
            if (!response)
            {
                response = ValidateTradeAccept(tradeOfferResponse.Offer);
            }
            return response;
        }        
        private bool AcceptTrade(TradeOffer tradeOffer)
        {
            var tradeOfferId = tradeOffer.Id;
            var url = "https://steamcommunity.com/tradeoffer/" + tradeOfferId + "/accept";
            var referer = "http://steamcommunity.com/tradeoffer/" + tradeOfferId + "/";
            var data = new NameValueCollection();
            data.Add("sessionid", SteamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("tradeofferid", tradeOfferId.ToString());
            data.Add("partner", tradeOffer.OtherSteamId.ToString());
            string response = RetryWebRequest(SteamWeb, url, "POST", data, true, referer);
            if (string.IsNullOrEmpty(response))
            {
                return false;// ValidateTradeAccept(tradeOffer);
            }
            else
            {
                try
                {
                    dynamic json = JsonConvert.DeserializeObject(response);
                    var id = json.tradeid;
                    return true;
                }
                catch
                {
                    return false;// ValidateTradeAccept(tradeOffer);
                }
            }
        }

        /// <summary>
        /// Declines a pending trade offer
        /// </summary>
        /// <param name="tradeOffer">The ID of the trade offer</param>
        /// <returns>True if successful, false if not</returns>
        public bool DeclineTrade(ulong tradeOfferId)
        {
            return DeclineTrade(GetTradeOffer(tradeOfferId).Offer);
        }        
        public bool DeclineTrade(TradeOffer tradeOffer)
        {
            var url = "https://steamcommunity.com/tradeoffer/" + tradeOffer.Id + "/decline";
            var referer = "http://steamcommunity.com/";
            var data = new NameValueCollection();
            data.Add("sessionid", SteamWeb.SessionId);
            data.Add("serverid", "1");
            string response = RetryWebRequest(SteamWeb, url, "POST", data, true, referer);
            try
            {
                dynamic json = JsonConvert.DeserializeObject(response);
                var id = json.tradeofferid;
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get a list of incoming trade offers.
        /// </summary>
        /// <returns>An 'int' list of trade offer IDs</returns>
        public List<ulong> GetIncomingTradeOffers()
        {
            List<ulong> IncomingTradeOffers = new List<ulong>();
            var url = "http://steamcommunity.com/profiles/" + BotId.ConvertToUInt64() + "/tradeoffers/";
            var html = RetryWebRequest(SteamWeb, url, "GET", null);
            var reg = new Regex("ShowTradeOffer\\((.*?)\\);");
            var matches = reg.Matches(html);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var tradeId = Convert.ToUInt64(match.Groups[1].Value.Replace("'", ""));
                    if (!IncomingTradeOffers.Contains(tradeId))
                        IncomingTradeOffers.Add(tradeId);
                }
            }
            return IncomingTradeOffers;
        }

        public GetTradeOfferAPI.GetTradeOfferResponse GetTradeOffer(ulong tradeOfferId)
        {
            string url = String.Format("https://api.steampowered.com/IEconService/GetTradeOffer/v1/?key={0}&tradeofferid={1}&language={2}", AccountApiKey, tradeOfferId, "en_us");
            string response = RetryWebRequest(SteamWeb, url, "GET", null, false, "http://steamcommunity.com");
            var result = JsonConvert.DeserializeObject<GetTradeOfferAPI>(response);
            if (result.Response != null && result.Response.Offer != null)
            {
                return result.Response;
            }
            return null;
        }

        public List<TradeOffer> GetTradeOffers()
        {
            var temp = new List<TradeOffer>();
            var url = "https://api.steampowered.com/IEconService/GetTradeOffers/v1/?key=" + AccountApiKey + "&get_sent_offers=1&get_received_offers=1&active_only=0";
            var response = RetryWebRequest(SteamWeb, url, "GET", null, false, "http://steamcommunity.com");
            var json = JsonConvert.DeserializeObject<dynamic>(response);
            var sentTradeOffers = json.response.trade_offers_sent;
            if (sentTradeOffers != null)
            {
                foreach (var tradeOffer in sentTradeOffers)
                {
                    temp.Add(JsonConvert.DeserializeObject<TradeOffer> (Convert.ToString (tradeOffer)));
                }
            }
            var receivedTradeOffers = json.response.trade_offers_received;
            if (receivedTradeOffers != null)
            {
                foreach (var tradeOffer in receivedTradeOffers)
                {
                    temp.Add(JsonConvert.DeserializeObject<TradeOffer> (Convert.ToString (tradeOffer)));
                }
            }
            return temp;
        }

        public enum TradeOfferState
        {
            Invalid = 1,
            Active = 2,
            Accepted = 3,
            Countered = 4,
            Expired = 5,
            Canceled = 6,
            Declined = 7,
            InvalidItems = 8
        }

        public class GetTradeOfferAPI
        {
            [JsonProperty("response")]
            public GetTradeOfferResponse Response { get; set; }

            public class GetTradeOfferResponse
            {
                [JsonProperty("offer")]
                public TradeOffer Offer { get; set; }

                [JsonProperty("descriptions")]
                public TradeOfferDescriptions[] Descriptions { get; set; }
            }
        }

        public class TradeOffer
        {
            [OnDeserialized()]
            internal void OnDeserializedMethod(StreamingContext context)
            {
                if (ItemsToReceive == null) {
                    ItemsToReceive = new TradeOffers.TradeOffer.CEconAsset [0];
                }
                if (ItemsToGive == null) {
                    ItemsToGive = new TradeOffers.TradeOffer.CEconAsset [0];
                }
            }

            [JsonProperty("tradeofferid")]
            public ulong Id { get; set; }

            [JsonProperty("accountid_other")]
            public ulong OtherAccountId { get; set; }
            
            public ulong OtherSteamId
            {
                get
                {
                    return new SteamID(String.Format("STEAM_0:{0}:{1}", OtherAccountId & 1, OtherAccountId >> 1)).ConvertToUInt64();
                }
                set
                {
                    OtherSteamId = value;
                }
            }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("expiration_time")]
            public ulong ExpirationTime { get; set; }

            [JsonProperty("trade_offer_state")]
            private int state { get; set; }
            public TradeOfferState State { get { return (TradeOfferState)state; } set { state = (int)value; } }

            [JsonProperty("items_to_give")]
            public CEconAsset[] ItemsToGive { get; set; }

            [JsonProperty("items_to_receive")]
            public CEconAsset[] ItemsToReceive { get; set; }

            [JsonProperty("is_our_offer")]
            public bool IsOurOffer { get; set; }

            [JsonProperty("time_created")]
            public ulong TimeCreated { get; set; }

            [JsonProperty("time_updated")]
            public ulong TimeUpdated { get; set; }

            [JsonProperty("from_real_time_trade")]
            public bool FromRealTimeTrade { get; set; }

            public class CEconAsset
            {
                [JsonProperty("appid")]
                public int AppId { get; set; }

                [JsonProperty("contextid")]
                public ulong ContextId { get; set; }

                [JsonProperty("assetid")]
                public ulong AssetId { get; set; }

                [JsonProperty("currencyid")]
                public ulong CurrencyId { get; set; }

                [JsonProperty("classid")]
                public ulong ClassId { get; set; }

                [JsonProperty("instanceid")]
                public ulong InstanceId { get; set; }

                [JsonProperty("amount")]
                public int Amount { get; set; }

                [JsonProperty("missing")]
                public bool IsMissing { get; set; }
            }
        }        

        public class TradeOfferDescriptions
        {
            [JsonProperty("appid")]
            public int AppId { get; set; }

            [JsonProperty("classid")]
            public long ClassId { get; set; }

            [JsonProperty("instanceid")]
            public long InstanceId { get; set; }

            [JsonProperty("currency")]
            public bool IsCurrency { get; set; }

            [JsonProperty("background_color")]
            public string BackgroundColor { get; set; }

            [JsonProperty("icon_url")]
            public string IconUrl { get; set; }

            [JsonProperty("icon_url_large")]
            public string IconUrlLarge { get; set; }

            [JsonProperty("descriptions")]
            public DescriptionsData[] Descriptions { get; set; }

            [JsonProperty("tradable")]
            public bool IsTradable { get; set; }

            [JsonProperty("actions")]
            public ActionsData[] Actions { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("name_color")]
            public string NameColor { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("market_name")]
            public string MarketName { get; set; }

            [JsonProperty("market_hash_name")]
            public string MarketHashName { get; set; }

            [JsonProperty("market_actions")]
            public MarketActionsData[] MarketActions { get; set; }

            [JsonProperty("commodity")]
            public bool IsCommodity { get; set; }

            public class DescriptionsData
            {
                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("value")]
                public string Value { get; set; }
            }

            public class ActionsData
            {
                [JsonProperty("link")]
                public string Link { get; set; }

                [JsonProperty("name")]
                public string Name { get; set; }
            }

            public class MarketActionsData
            {
                [JsonProperty("link")]
                public string Link { get; set; }

                [JsonProperty("name")]
                public string Name { get; set; }
            }
        }

        /// <summary>
        /// Manually validate if a trade offer went through by checking /inventoryhistory/
        /// </summary>
        /// <param name="tradeOffer">A 'TradeStatus' object</param>
        /// <returns>True if the trade offer was successfully accepted, false if otherwise</returns>
        public bool ValidateTradeAccept(TradeOffer tradeOffer)
        {
            var history = GetTradeHistory(1);
            foreach (var completedTrade in history)
            {
                var givenItems = new List<Trade.TradeAsset>();
                foreach (var myItem in tradeOffer.ItemsToGive)
                {
                    var genericItem = new Trade.TradeAsset(myItem.AppId, myItem.ContextId, myItem.AssetId, myItem.Amount);
                    givenItems.Add(genericItem);
                }
                var receivedItems = new List<Trade.TradeAsset>();
                foreach (var otherItem in tradeOffer.ItemsToReceive)
                {
                    var genericItem = new Trade.TradeAsset(otherItem.AppId, otherItem.ContextId, otherItem.AssetId, otherItem.Amount);
                    receivedItems.Add(genericItem);
                }
                if (givenItems.Count == completedTrade.GivenItems.Count && receivedItems.Count == completedTrade.ReceivedItems.Count)
                {
                    foreach (var item in completedTrade.GivenItems)
                    {
                        var genericItem = new Trade.TradeAsset(item.appid, item.contextid, item.assetid, item.amount);
                        if (!givenItems.Contains(genericItem))
                            return false;
                    }
                    foreach (var item in completedTrade.ReceivedItems)
                    {
                        var genericItem = new Trade.TradeAsset(item.appid, item.contextid, item.assetid, item.amount);
                        if (!receivedItems.Contains(genericItem))
                            return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves completed trades from /inventoryhistory/
        /// </summary>
        /// <param name="limit">Max number of trades to retrieve</param>
        /// <returns>A List of 'TradeHistory' objects</returns>
        public List<TradeHistory> GetTradeHistory(int limit = 0)
        {
            // most recent trade is first
            List<TradeHistory> TradeHistoryList = new List<TradeHistory>();
            var url = "http://steamcommunity.com/profiles/" + BotId.ConvertToUInt64() + "/inventoryhistory/";
            var html = RetryWebRequest(SteamWeb, url, "GET", null);
            // TODO: handle rgHistoryCurrency as well
            Regex reg = new Regex("rgHistoryInventory = (.*?)};");
            Match m = reg.Match(html);
            if (m.Success)
            {
                var json = m.Groups[1].Value + "}";
                var schemaResult = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<ulong, Dictionary<ulong, GenericInventory.Inventory.Item>>>>(json);
                var trades = new Regex("HistoryPageCreateItemHover\\((.*?)\\);");
                var tradeMatches = trades.Matches(html);
                foreach (Match match in tradeMatches)
                {
                    if (match.Success)
                    {
                        var tradeHistoryItem = new TradeHistory();
                        tradeHistoryItem.ReceivedItems = new List<Trade.TradeAsset>();
                        tradeHistoryItem.GivenItems = new List<Trade.TradeAsset>();
                        var historyString = match.Groups[1].Value.Replace("'", "").Replace(" ", "");
                        var split = historyString.Split(',');
                        var tradeString = split[0];
                        var tradeStringSplit = tradeString.Split('_');
                        var tradeNum = Convert.ToInt32(tradeStringSplit[0].Replace("trade", ""));
                        if (limit > 0 && tradeNum >= limit) break;
                        var appId = Convert.ToInt32(split[1]);
                        var contextId = Convert.ToUInt64(split[2]);
                        var itemId = Convert.ToUInt64(split[3]);
                        var amount = Convert.ToInt32(split[4]);
                        var historyItem = schemaResult[appId][contextId][itemId];
                        var genericItem = new Trade.TradeAsset(appId, contextId, itemId, amount);
                        // given item has ownerId of 0
                        // received item has ownerId of own SteamID
                        if (historyItem.OwnerId == 0)
                            tradeHistoryItem.GivenItems.Add(genericItem);
                        else
                            tradeHistoryItem.ReceivedItems.Add(genericItem);
                        TradeHistoryList.Add(tradeHistoryItem);
                    }
                }
            }
            return TradeHistoryList;
        }

        public class TradeHistory
        {
            public List<Trade.TradeAsset> ReceivedItems { get; set; }
            public List<Trade.TradeAsset> GivenItems { get; set; }
        }

        public void AddPendingTradeOfferToList(ulong tradeOfferId)
        {
            OurPendingTradeOffers.Add(tradeOfferId);
        }
        private void RemovePendingTradeOfferFromList(ulong tradeOfferId)
        {
            OurPendingTradeOffers.Remove(tradeOfferId);
        }

        public void StopCheckingPendingTradeOffers()
        {
            this.ShouldCheckPendingTradeOffers = false;
        }

        private void CheckPendingTradeOffers()
        {
            while (ShouldCheckPendingTradeOffers)
            {
                var tradeOffers = GetTradeOffers();
                foreach (var tradeOffer in tradeOffers)
                {
                    if (tradeOffer.IsOurOffer && OurPendingTradeOffers.Contains(tradeOffer.Id))
                    {
                        if (tradeOffer.State != TradeOfferState.Active)
                        {
                            var args = new TradeOfferEventArgs();
                            args.TradeOffer = tradeOffer;

                            // check if trade offer has been accepted/declined, or items unavailable (manually validate)
                            if (tradeOffer.State == TradeOfferState.Accepted)
                            {
                                // fire event                            
                                OnTradeOfferAccepted(args);
                                // remove from list
                                OurPendingTradeOffers.Remove(tradeOffer.Id);
                            }
                            else if (tradeOffer.State != TradeOfferState.Active && tradeOffer.State != TradeOfferState.Accepted)
                            {
                                if (tradeOffer.State == TradeOfferState.Invalid || tradeOffer.State == TradeOfferState.InvalidItems)
                                {
                                    // invalid, handle manually
                                }
                                else
                                {
                                    // fire event
                                    OnTradeOfferDeclined(args);
                                    // remove from list
                                    OurPendingTradeOffers.Remove(tradeOffer.Id);
                                }
                            }
                        }
                    }
                    else if (!tradeOffer.IsOurOffer && !ReceivedPendingTradeOffers.Contains(tradeOffer.Id))
                    {
                        var args = new TradeOfferEventArgs();
                        args.TradeOffer = tradeOffer;

                        if (tradeOffer.State == TradeOfferState.Active)
                        {
                            ReceivedPendingTradeOffers.Add(tradeOffer.Id);
                            OnTradeOfferReceived(args);
                        }
                        else
                        {
                            ReceivedPendingTradeOffers.Remove(tradeOffer.Id);
                        }
                    }
                }
                System.Threading.Thread.Sleep(TradeOfferRefreshRate);
            }
        }

        protected virtual void OnTradeOfferReceived(TradeOfferEventArgs e)
        {
            TradeOfferStatusEventHandler handler = TradeOfferReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void OnTradeOfferAccepted(TradeOfferEventArgs e)
        {
            TradeOfferStatusEventHandler handler = TradeOfferAccepted;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void OnTradeOfferDeclined(TradeOfferEventArgs e)
        {
            TradeOfferStatusEventHandler handler = TradeOfferDeclined;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event TradeOfferStatusEventHandler TradeOfferReceived;
        public event TradeOfferStatusEventHandler TradeOfferAccepted;
        public event TradeOfferStatusEventHandler TradeOfferDeclined;

        public class TradeOfferEventArgs : EventArgs
        {
            public TradeOffer TradeOffer { get; set; }
        }
        public delegate void TradeOfferStatusEventHandler(Object sender, TradeOfferEventArgs e);

        private static string RetryWebRequest(SteamWeb steamWeb, string url, string method, NameValueCollection data, bool ajax = false, string referer = "")
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var response = steamWeb.Request(url, method, data, ajax, referer);
                    using (System.IO.Stream responseStream = response.GetResponseStream())
                    {
                        using (var reader = new System.IO.StreamReader(responseStream))
                        {
                            string result = reader.ReadToEnd();
                            if (string.IsNullOrEmpty(result))
                            {
                                Console.WriteLine("Web request failed (status: {0}). Retrying...", response.StatusCode);
                                System.Threading.Thread.Sleep(1000);
                            }
                            else
                            {
                                return result;
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    try
                    {
                        if (ex.Status == WebExceptionStatus.ProtocolError)
                        {
                            Console.WriteLine("Status Code: {0}, {1}", (int)((HttpWebResponse)ex.Response).StatusCode, ((HttpWebResponse)ex.Response).StatusDescription);
                        }
                        Console.WriteLine("Error: {0}", new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
                    }
                    catch
                    {

                    }                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return "";
        }
    }
}
