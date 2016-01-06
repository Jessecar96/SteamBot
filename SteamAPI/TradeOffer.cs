using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SteamKit2;
using System.Net;
using Newtonsoft.Json;

namespace SteamAPI
{
    public class TradeOffers
    {
        public SteamID BotId;
        private SteamWeb SteamWeb;
        public List<ulong> OurPendingTradeOffers;
        private List<ulong> HandledTradeOffers;
        private List<ulong> AwaitingConfirmationTradeOffers;
        private string AccountApiKey;
        private bool ShouldCheckPendingTradeOffers;
        private int TradeOfferRefreshRate;

        public TradeOffers(SteamID botId, SteamWeb steamWeb, string accountApiKey, int tradeOfferRefreshRate, List<ulong> pendingTradeOffers = null)
        {
            this.BotId = botId;
            this.SteamWeb = steamWeb;
            this.AccountApiKey = accountApiKey;
            this.ShouldCheckPendingTradeOffers = true;
            this.TradeOfferRefreshRate = tradeOfferRefreshRate;

            if (pendingTradeOffers == null)
                this.OurPendingTradeOffers = new List<ulong>();
            else
                this.OurPendingTradeOffers = pendingTradeOffers;

            this.HandledTradeOffers = new List<ulong>();
            this.AwaitingConfirmationTradeOffers = new List<ulong>();

            new Thread(CheckPendingTradeOffers).Start();
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
            return AcceptTrade(tradeOfferResponse.Offer);
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
                return ValidateTradeAccept(tradeOffer);
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
                    return ValidateTradeAccept(tradeOffer);
                }
            }
        }

        /// <summary>
        /// Declines a pending trade offer
        /// </summary>
        /// <param name="tradeOfferId">The trade offer ID</param>
        /// <returns>True if successful, false if not</returns>
        public bool DeclineTrade(TradeOffer tradeOffer)
        {
            return DeclineTrade(tradeOffer.Id);
        }
        /// <summary>
        /// Declines a pending trade offer
        /// </summary>
        /// <param name="tradeOffer">The trade offer object</param>
        /// <returns>True if successful, false if not</returns>
        public bool DeclineTrade(ulong tradeOfferId)
        {
            var url = "https://steamcommunity.com/tradeoffer/" + tradeOfferId + "/decline";
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
        /// Cancels a pending sent trade offer
        /// </summary>
        /// <param name="tradeOffer">The trade offer object</param>
        /// <returns>True if successful, false if not</returns>
        public bool CancelTrade(TradeOffer tradeOffer)
        {
            return CancelTrade(tradeOffer.Id);
        }
        /// <summary>
        /// Cancels a pending sent trade offer
        /// </summary>
        /// <param name="tradeOfferId">The trade offer ID</param>
        /// <returns>True if successful, false if not</returns>
        public bool CancelTrade(ulong tradeOfferId)
        {
            var url = "https://steamcommunity.com/tradeoffer/" + tradeOfferId + "/cancel";
            var referer = "http://steamcommunity.com/";
            var data = new NameValueCollection();
            data.Add("sessionid", SteamWeb.SessionId);
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

        /// <summary>
        /// Get list of trade offers from API
        /// </summary>
        /// <param name="getActive">Set this to true to get active-only trade offers</param>
        /// <returns>list of trade offers</returns>
        public List<TradeOffer> GetTradeOffers(bool getActive = false)
        {
            var temp = new List<TradeOffer>();
            var url = "https://api.steampowered.com/IEconService/GetTradeOffers/v1/?key=" + AccountApiKey + "&get_sent_offers=1&get_received_offers=1";
            if (getActive)
            {
                url += "&active_only=1&time_historical_cutoff=" + (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }
            else
            {
                url += "&active_only=0";
            }
            var response = RetryWebRequest(SteamWeb, url, "GET", null, false, "http://steamcommunity.com");
            var json = JsonConvert.DeserializeObject<dynamic>(response);
            var sentTradeOffers = json.response.trade_offers_sent;
            if (sentTradeOffers != null)
            {
                foreach (var tradeOffer in sentTradeOffers)
                {
                    TradeOffer tempTrade = JsonConvert.DeserializeObject<TradeOffer>(Convert.ToString(tradeOffer));
                    temp.Add(tempTrade);
                }
            }
            var receivedTradeOffers = json.response.trade_offers_received;
            if (receivedTradeOffers != null)
            {
                foreach (var tradeOffer in receivedTradeOffers)
                {
                    TradeOffer tempTrade = JsonConvert.DeserializeObject<TradeOffer>(Convert.ToString(tradeOffer));
                    temp.Add(tempTrade);
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
            InvalidItems = 8,
            NeedsConfirmation = 9,
            CanceledBySecondFactor = 10,
            InEscrow = 11
        }

        public enum TradeOfferConfirmationMethod
        {
            Invalid = 0,
            Email = 1,
            MobileApp = 2
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
            private CEconAsset[] itemsToGive { get; set; }
            public CEconAsset[] ItemsToGive
            {
                get
                {
                    if (itemsToGive == null)
                    {
                        return new CEconAsset[0];
                    }
                    return itemsToGive;
                }
                set
                {
                    itemsToGive = value;
                }
            }

            [JsonProperty("items_to_receive")]
            private CEconAsset[] itemsToReceive { get; set; }
            public CEconAsset[] ItemsToReceive
            {
                get
                {
                    if (itemsToReceive == null)
                    {
                        return new CEconAsset[0];
                    }
                    return itemsToReceive;
                }
                set
                {
                    itemsToReceive = value;
                }
            }

            [JsonProperty("is_our_offer")]
            public bool IsOurOffer { get; set; }

            [JsonProperty("time_created")]
            public int TimeCreated { get; set; }

            [JsonProperty("time_updated")]
            public int TimeUpdated { get; set; }

            [JsonProperty("from_real_time_trade")]
            public bool FromRealTimeTrade { get; set; }

            [JsonProperty("escrow_end_date")]
            public int EscrowEndDate { get; set; }

            [JsonProperty("confirmation_method")]
            private int confirmationMethod { get; set; }
            public TradeOfferConfirmationMethod ConfirmationMethod { get { return (TradeOfferConfirmationMethod)confirmationMethod; } set { confirmationMethod = (int)value; } }

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
        /// <param name="tradeOffer">A 'TradeOffer' object</param>
        /// <returns>True if the trade offer was successfully accepted, false if otherwise</returns>
        public bool ValidateTradeAccept(TradeOffer tradeOffer)
        {
            try
            {
                var history = GetTradeHistory();
                foreach (var completedTrade in history)
                {
                    if (tradeOffer.ItemsToGive.Length == completedTrade.GivenItems.Count && tradeOffer.ItemsToReceive.Length == completedTrade.ReceivedItems.Count)
                    {
                        var numFoundGivenItems = 0;
                        var numFoundReceivedItems = 0;
                        var foundItemIds = new List<ulong>();
                        foreach (var historyItem in completedTrade.GivenItems)
                        {
                            foreach (var tradeOfferItem in tradeOffer.ItemsToGive)
                            {
                                if (tradeOfferItem.ClassId == historyItem.ClassId && tradeOfferItem.InstanceId == historyItem.InstanceId)
                                {
                                    if (!foundItemIds.Contains(tradeOfferItem.AssetId))
                                    {
                                        foundItemIds.Add(tradeOfferItem.AssetId);
                                        numFoundGivenItems++;
                                    }
                                }
                            }
                        }
                        foreach (var historyItem in completedTrade.ReceivedItems)
                        {
                            foreach (var tradeOfferItem in tradeOffer.ItemsToReceive)
                            {
                                if (tradeOfferItem.ClassId == historyItem.ClassId && tradeOfferItem.InstanceId == historyItem.InstanceId)
                                {
                                    if (!foundItemIds.Contains(tradeOfferItem.AssetId))
                                    {
                                        foundItemIds.Add(tradeOfferItem.AssetId);
                                        numFoundReceivedItems++;
                                    }
                                }
                            }
                        }
                        if (numFoundGivenItems == tradeOffer.ItemsToGive.Length && numFoundReceivedItems == tradeOffer.ItemsToReceive.Length)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error validating trade:");
                Console.WriteLine(ex);
            }
            return false;
        }

        /// <summary>
        /// Retrieves completed trades from /inventoryhistory/
        /// </summary>
        /// <param name="limit">Max number of trades to retrieve</param>
        /// <returns>A List of 'TradeHistory' objects</returns>
        public List<TradeHistory> GetTradeHistory(int limit = 0, int numPages = 1)
        {
            var tradeHistoryPages = new Dictionary<int, TradeHistory[]>();
            for (int i = 0; i < numPages; i++)
            {
                var tradeHistoryPageList = new TradeHistory[30];
                try
                {
                    var url = "http://steamcommunity.com/profiles/" + BotId.ConvertToUInt64() + "/inventoryhistory/?p=" + i;
                    var html = RetryWebRequest(SteamWeb, url, "GET", null);
                    // TODO: handle rgHistoryCurrency as well
                    Regex reg = new Regex("rgHistoryInventory = (.*?)};");
                    Match m = reg.Match(html);
                    if (m.Success)
                    {
                        var json = m.Groups[1].Value + "}";
                        var schemaResult = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<ulong, Dictionary<ulong, TradeHistory.HistoryItem>>>>(json);
                        var trades = new Regex("HistoryPageCreateItemHover\\((.*?)\\);");
                        var tradeMatches = trades.Matches(html);
                        foreach (Match match in tradeMatches)
                        {
                            if (match.Success)
                            {
                                var historyString = match.Groups[1].Value.Replace("'", "").Replace(" ", "");
                                var split = historyString.Split(',');
                                var tradeString = split[0];
                                var tradeStringSplit = tradeString.Split('_');
                                var tradeNum = Convert.ToInt32(tradeStringSplit[0].Replace("trade", ""));
                                if (limit > 0 && tradeNum >= limit) break;
                                if (tradeHistoryPageList[tradeNum] == null)
                                {
                                    tradeHistoryPageList[tradeNum] = new TradeHistory();
                                }
                                var tradeHistoryItem = tradeHistoryPageList[tradeNum];
                                var appId = Convert.ToInt32(split[1]);
                                var contextId = Convert.ToUInt64(split[2]);
                                var itemId = Convert.ToUInt64(split[3]);
                                var amount = Convert.ToInt32(split[4]);
                                var historyItem = schemaResult[appId][contextId][itemId];
                                if (historyItem.OwnerId == 0)
                                    tradeHistoryItem.ReceivedItems.Add(historyItem);
                                else
                                    tradeHistoryItem.GivenItems.Add(historyItem);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving trade history:");
                    Console.WriteLine(ex);
                }
                tradeHistoryPages.Add(i, tradeHistoryPageList);
            }
            var tradeHistoryList = new List<TradeHistory>();
            foreach (var tradeHistoryPage in tradeHistoryPages.Values)
            {
                foreach (var tradeHistory in tradeHistoryPage)
                {
                    tradeHistoryList.Add(tradeHistory);
                }
            }
            return tradeHistoryList;
        }

        public class TradeHistory
        {
            public List<HistoryItem> ReceivedItems { get; set; }
            public List<HistoryItem> GivenItems { get; set; }

            public TradeHistory()
            {
                ReceivedItems = new List<HistoryItem>();
                GivenItems = new List<HistoryItem>();
            }

            public class HistoryItem
            {
                private Trade.TradeAsset tradeAsset = null;
                public Trade.TradeAsset TradeAsset
                {
                    get
                    {
                        if (tradeAsset == null)
                        {
                            tradeAsset = new Trade.TradeAsset(AppId, ContextId, Id, Amount);
                        }
                        return tradeAsset;
                    }
                    set
                    {
                        tradeAsset = value;
                    }
                }

                [JsonProperty("id")]
                public ulong Id { get; set; }

                [JsonProperty("contextid")]
                public ulong ContextId { get; set; }

                [JsonProperty("amount")]
                public int Amount { get; set; }

                [JsonProperty("owner")]
                private dynamic owner { get; set; }
                public ulong OwnerId
                {
                    get
                    {
                        ulong ownerId = 0;
                        ulong.TryParse(Convert.ToString(owner), out ownerId);
                        return ownerId;
                    }
                    set
                    {
                        owner = value.ToString();
                    }
                }

                [JsonProperty("appid")]
                public int AppId { get; set; }

                [JsonProperty("classid")]
                public ulong ClassId { get; set; }

                [JsonProperty("instanceid")]
                public ulong InstanceId { get; set; }

                [JsonProperty("is_currency")]
                public bool IsCurrency { get; set; }

                [JsonProperty("icon_url")]
                public string IconUrl { get; set; }

                [JsonProperty("icon_url_large")]
                public string IconUrlLarge { get; set; }

                [JsonProperty("icon_drag_url")]
                public string IconDragUrl { get; set; }

                [JsonProperty("name")]
                public string Name { get; set; }

                [JsonProperty("market_hash_name")]
                public string MarketHashName { get; set; }

                [JsonProperty("market_name")]
                public string MarketName { get; set; }

                [JsonProperty("name_color")]
                public string NameColor { get; set; }

                [JsonProperty("background_color")]
                public string BackgroundColor { get; set; }

                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("tradable")]
                public bool IsTradable { get; set; }

                [JsonProperty("marketable")]
                public bool IsMarketable { get; set; }

                [JsonProperty("commodity")]
                public bool IsCommodity { get; set; }

                [JsonProperty("market_tradable_restriction")]
                public int MarketTradableRestriction { get; set; }

                [JsonProperty("market_marketable_restriction")]
                public int MarketMarketableRestriction { get; set; }

                [JsonProperty("fraudwarnings")]
                public dynamic FraudWarnings { get; set; }

                [JsonProperty("descriptions")]
                private dynamic descriptions { get; set; }
                public DescriptionsData[] Descriptions
                {
                    get
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(descriptions)))
                        {
                            try
                            {
                                return JsonConvert.DeserializeObject<DescriptionsData[]>(descriptions);
                            }
                            catch
                            {

                            }
                        }
                        return new DescriptionsData[0];
                    }
                    set
                    {
                        descriptions = JsonConvert.SerializeObject(value);
                    }
                }

                [JsonProperty("actions")]
                private dynamic actions { get; set; }
                public ActionsData[] Actions
                {
                    get
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(actions)))
                        {
                            try
                            {
                                return JsonConvert.DeserializeObject<ActionsData[]>(actions);
                            }
                            catch
                            {

                            }
                        }
                        return new ActionsData[0];
                    }
                    set
                    {
                        descriptions = JsonConvert.SerializeObject(value);
                    }
                }

                [JsonProperty("tags")]
                public TagsData[] Tags { get; set; }

                [JsonProperty("app_data")]
                public dynamic AppData { get; set; }

                public class DescriptionsData
                {
                    [JsonProperty("type")]
                    public string Type { get; set; }

                    [JsonProperty("value")]
                    public string Value { get; set; }

                    [JsonProperty("color")]
                    public string Color { get; set; }

                    [JsonProperty("app_data")]
                    public dynamic AppData { get; set; }
                }

                public class ActionsData
                {
                    [JsonProperty("name")]
                    public string Name { get; set; }

                    [JsonProperty("link")]
                    public string Link { get; set; }
                }

                public class TagsData
                {
                    [JsonProperty("internal_name")]
                    public string InternalName { get; set; }

                    [JsonProperty("name")]
                    public string Name { get; set; }

                    [JsonProperty("category")]
                    public string Category { get; set; }

                    [JsonProperty("category_name")]
                    public string CategoryName { get; set; }

                    [JsonProperty("color")]
                    public string Color { get; set; }
                }
            }
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
            new Thread(() =>
            {
                while (ShouldCheckPendingTradeOffers)
                {
                    var tradeOffers = GetTradeOffers(true);
                    foreach (var tradeOffer in tradeOffers)
                    {
                        if (tradeOffer.IsOurOffer)
                        {
                            if (!OurPendingTradeOffers.Contains(tradeOffer.Id))
                            {
                                OurPendingTradeOffers.Add(tradeOffer.Id);
                            }
                        }
                        else if (!tradeOffer.IsOurOffer)
                        {
                            var args = new TradeOfferEventArgs();
                            args.TradeOffer = tradeOffer;

                            if (tradeOffer.State == TradeOfferState.Active)
                            {
                                if (tradeOffer.ConfirmationMethod != TradeOfferConfirmationMethod.Invalid)
                                {
                                    OnTradeOfferNeedsConfirmation(args);
                                }
                                else
                                {
                                    OnTradeOfferReceived(args);
                                }                                
                            }
                        }
                    }
                    Thread.Sleep(TradeOfferRefreshRate);
                }
            }).Start();
            while (ShouldCheckPendingTradeOffers)
            {
                var checkingThreads = new List<Thread>();
                foreach (var tradeOfferId in OurPendingTradeOffers.ToList())
                {
                    var thread = new Thread(() =>
                    {
                        var pendingTradeOffer = GetTradeOffer(tradeOfferId);
                        if (pendingTradeOffer != null)
                        {
                            if (pendingTradeOffer.Offer.State != TradeOfferState.Active)
                            {
                                var args = new TradeOfferEventArgs();
                                args.TradeOffer = pendingTradeOffer.Offer;

                                // check if trade offer has been accepted/declined, or items unavailable (manually validate)
                                if (pendingTradeOffer.Offer.State == TradeOfferState.Accepted)
                                {
                                    // fire event                            
                                    OnTradeOfferAccepted(args);
                                    // remove from list
                                    OurPendingTradeOffers.Remove(pendingTradeOffer.Offer.Id);
                                }
                                else
                                {
                                    if (pendingTradeOffer.Offer.State == TradeOfferState.NeedsConfirmation)
                                    {
                                        // fire event
                                        OnTradeOfferNeedsConfirmation(args);
                                    }
                                    else if (pendingTradeOffer.Offer.State == TradeOfferState.Invalid || pendingTradeOffer.Offer.State == TradeOfferState.InvalidItems)
                                    {
                                        // fire event
                                        OnTradeOfferInvalid(args);
                                    }
                                    else if (pendingTradeOffer.Offer.State != TradeOfferState.InEscrow)
                                    {
                                        // fire event
                                        OnTradeOfferDeclined(args);
                                    }

                                    if (pendingTradeOffer.Offer.State != TradeOfferState.InEscrow)
                                    {
                                        // remove from list only if not in escrow
                                        OurPendingTradeOffers.Remove(pendingTradeOffer.Offer.Id);
                                    }                                    
                                }
                            }
                        }
                    });
                    checkingThreads.Add(thread);
                    thread.Start();
                }
                foreach (var thread in checkingThreads)
                {
                    thread.Join();
                }
                Thread.Sleep(TradeOfferRefreshRate);
            }
        }

        protected virtual void OnTradeOfferReceived(TradeOfferEventArgs e)
        {
            if (!HandledTradeOffers.Contains(e.TradeOffer.Id))
            {
                TradeOfferStatusEventHandler handler = TradeOfferReceived;
                if (handler != null)
                {
                    handler(this, e);
                }
                HandledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferAccepted(TradeOfferEventArgs e)
        {
            if (!HandledTradeOffers.Contains(e.TradeOffer.Id))
            {
                TradeOfferStatusEventHandler handler = TradeOfferAccepted;
                if (handler != null)
                {
                    handler(this, e);
                }
                HandledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferDeclined(TradeOfferEventArgs e)
        {
            if (!HandledTradeOffers.Contains(e.TradeOffer.Id))
            {
                TradeOfferStatusEventHandler handler = TradeOfferDeclined;
                if (handler != null)
                {
                    handler(this, e);
                }
                HandledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferInvalid(TradeOfferEventArgs e)
        {
            if (!HandledTradeOffers.Contains(e.TradeOffer.Id))
            {
                TradeOfferStatusEventHandler handler = TradeOfferInvalid;
                if (handler != null)
                {
                    handler(this, e);
                }
                HandledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferNeedsConfirmation(TradeOfferEventArgs e)
        {
            if (!AwaitingConfirmationTradeOffers.Contains(e.TradeOffer.Id))
            {
                TradeOfferStatusEventHandler handler = TradeOfferNeedsConfirmation;
                if (handler != null)
                {
                    handler(this, e);
                }
                AwaitingConfirmationTradeOffers.Add(e.TradeOffer.Id);
            }
        }

        public event TradeOfferStatusEventHandler TradeOfferReceived;
        public event TradeOfferStatusEventHandler TradeOfferAccepted;
        public event TradeOfferStatusEventHandler TradeOfferDeclined;
        public event TradeOfferStatusEventHandler TradeOfferInvalid;
        public event TradeOfferStatusEventHandler TradeOfferNeedsConfirmation;

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
                                Thread.Sleep(1000);
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
