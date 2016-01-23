using Newtonsoft.Json;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SteamTrade.TradeOffer
{
    public class TradeOffer
    {
        private OfferSession Session { get; set; }

        public SteamID PartnerSteamId { get; private set; }

        public TradeStatus Items { get; set; }

        public string TradeOfferId { get; private set; }

        public TradeOfferState OfferState { get; private set; }

        public bool IsOurOffer { get; private set; }

        public int TimeCreated { get; private set; }

        public int ExpirationTime { get; private set; }

        public int TimeUpdated { get; private set; }
        
        public string Message { get; private set; }

        public bool IsFirstOffer
        {
            get { return TimeCreated == TimeUpdated; }
        }

        public TradeOffer(OfferSession session, SteamID partnerSteamdId)
        {
            Items = new TradeStatus();
            IsOurOffer = true;
            OfferState = TradeOfferState.TradeOfferStateUnknown;
            this.Session = session;
            this.PartnerSteamId = partnerSteamdId;
        }

        public TradeOffer(OfferSession session, Offer offer)
        {
            var myAssets = new List<TradeStatusUser.TradeAsset>();
            var myMissingAssets = new List<TradeStatusUser.TradeAsset>();
            var theirAssets = new List<TradeStatusUser.TradeAsset>();
            var theirMissingAssets = new List<TradeStatusUser.TradeAsset>();
            if (offer.ItemsToGive != null)
            {
                foreach (var asset in offer.ItemsToGive)
                {
                    var tradeAsset = new TradeStatusUser.TradeAsset();
                    //todo: for currency items we need to check descriptions for currency bool and use the appropriate method
                    tradeAsset.CreateItemAsset(Convert.ToInt64(asset.AppId), Convert.ToInt64(asset.ContextId),
                        Convert.ToInt64(asset.AssetId), Convert.ToInt64(asset.Amount));
                    //todo: for missing assets we should store them somewhere else? if offer state is active we shouldn't be here though
                    if (!asset.IsMissing)
                    {
                        myAssets.Add(tradeAsset);
                    }
                    else
                    {
                        myMissingAssets.Add(tradeAsset);
                    }
                }
            }
            if (offer.ItemsToReceive != null)
            {
                foreach (var asset in offer.ItemsToReceive)
                {
                    var tradeAsset = new TradeStatusUser.TradeAsset();
                    tradeAsset.CreateItemAsset(Convert.ToInt64(asset.AppId), Convert.ToInt64(asset.ContextId),
                        Convert.ToInt64(asset.AssetId), Convert.ToInt64(asset.Amount));
                    if (!asset.IsMissing)
                    {
                        theirAssets.Add(tradeAsset);
                    }
                    else
                    {
                        theirMissingAssets.Add(tradeAsset);
                    }
                }
            }
            this.Session = session;
            //assume public individual
            PartnerSteamId = new SteamID(Convert.ToUInt32(offer.AccountIdOther), EUniverse.Public, EAccountType.Individual);
            TradeOfferId = offer.TradeOfferId;
            OfferState = offer.TradeOfferState;
            IsOurOffer = offer.IsOurOffer;
            ExpirationTime = offer.ExpirationTime;
            TimeCreated = offer.TimeCreated;
            TimeUpdated = offer.TimeUpdated;
            Message = offer.Message;
            Items = new TradeStatus(myAssets, theirAssets);
        }

        /// <summary>
        /// Counter an existing offer with an updated offer
        /// </summary>
        /// <param name="newTradeOfferId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool CounterOffer(out string newTradeOfferId, string message = "")
        {
            newTradeOfferId = String.Empty;
            if (!String.IsNullOrEmpty(TradeOfferId) && !IsOurOffer && OfferState == TradeOfferState.TradeOfferStateActive && Items.NewVersion)
            {
                return Session.CounterOffer(message, PartnerSteamId, this.Items, out newTradeOfferId, TradeOfferId);
            }
            //todo: log
            Debug.WriteLine("Can't counter offer a trade that doesn't have an offerid, is ours or isn't active");
            return false;
        }

        /// <summary>
        /// Send a new trade offer
        /// </summary>
        /// <param name="offerId">The trade offer id if successully created</param>
        /// <param name="message">Optional message to included with the trade offer</param>
        /// <returns>true if successfully sent, otherwise false</returns>
        public bool Send(out string offerId, string message = "")
        {
            offerId = String.Empty;
            if (TradeOfferId == null)
            {
                return Session.SendTradeOffer(message, PartnerSteamId, this.Items, out offerId);
            }
            //todo: log
            Debug.WriteLine("Can't send a trade offer that already exists.");
            return false;
        }

        /// <summary>
        /// Send a new trade offer using a token
        /// </summary>
        /// <param name="offerId">The trade offer id if successully created</param>
        /// <param name="token">The token of the partner</param>
        /// <param name="message">Optional message to included with the trade offer</param>
        /// <returns></returns>
        public bool SendWithToken(out string offerId, string token, string message = "")
        {
            offerId = String.Empty;
            if (TradeOfferId == null)
            {
                return Session.SendTradeOfferWithToken(message, PartnerSteamId, this.Items, token, out offerId);
            }
            //todo: log
            Debug.WriteLine("Can't send a trade offer that already exists.");
            return false;
        }

        /// <summary>
        /// Accepts the current offer
        /// </summary>        
        /// <returns>TradeOfferAcceptResponse object containing accept result</returns>
        public TradeOfferAcceptResponse Accept()
        {
            if (TradeOfferId == null)
            {
                return new TradeOfferAcceptResponse { TradeError = "Can't accept a trade without a tradeofferid" };                
            }
            if (!IsOurOffer && OfferState == TradeOfferState.TradeOfferStateActive)
            {
                return Session.Accept(TradeOfferId);
            }
            //todo: log wrong state
            return new TradeOfferAcceptResponse { TradeError = "Can't accept a trade that is not active" };            
        }


        /// <summary>
        /// Accepts the current offer. Old signature for compatibility
        /// </summary>
        /// <param name="tradeId">the tradeid if successful</param>
        /// <returns>true if successful, otherwise false</returns>
        [Obsolete("Use TradeOfferAcceptResponse Accept()")]
        public bool Accept(out string tradeId)
        {
            tradeId = String.Empty;
            if (TradeOfferId == null) 
            {   
                //throw like original function did             
                throw new ArgumentException("TradeOfferId");
            }
            else 
            {
                TradeOfferAcceptResponse result = Accept();
                if (result.Accepted) 
                {
                    tradeId = result.TradeId;
                }
                return result.Accepted;
            }            
        }

        /// <summary>
        /// Decline the current offer
        /// </summary>
        /// <returns>true if successful, otherwise false</returns>
        public bool Decline()
        {
            if (TradeOfferId == null)
            {
                Debug.WriteLine("Can't decline a trade without a tradeofferid");
                throw new ArgumentException("TradeOfferId");
            }
            if (!IsOurOffer && OfferState == TradeOfferState.TradeOfferStateActive)
            {
                return Session.Decline(TradeOfferId);
            }
            //todo: log wrong state
            Debug.WriteLine("Can't decline a trade that is not active");
            return false;
        }

        /// <summary>
        /// Cancel the current offer
        /// </summary>
        /// <returns>true if successful, otherwise false</returns>
        public bool Cancel()
        {
            if (TradeOfferId == null)
            {
                Debug.WriteLine("Can't cancel a trade without a tradeofferid");
                throw new ArgumentException("TradeOfferId");
            }
            if (IsOurOffer && OfferState == TradeOfferState.TradeOfferStateActive)
            {
                return Session.Cancel(TradeOfferId);
            }
            //todo: log wrong state
            Debug.WriteLine("Can't cancel a trade that is not active and ours");
            return false;
        }

        /// <summary>
        /// Stores the state of the assets being offered
        /// </summary>
        public class TradeStatus
        {
            [JsonProperty("newversion")]
            public bool NewVersion { get; private set; }

            [JsonProperty("version")]
            private int Version { get; set; }

            [JsonProperty("me")]
            private TradeStatusUser MyOfferedItems { get; set; }

            [JsonProperty("them")]
            private TradeStatusUser TheirOfferedItems { get; set; }

            public TradeStatus()
            {
                Version = 1;
                MyOfferedItems = new TradeStatusUser();
                TheirOfferedItems = new TradeStatusUser();
            }

            public TradeStatus(List<TradeStatusUser.TradeAsset> myItems, List<TradeStatusUser.TradeAsset> theirItems)
            {
                Version = 1;
                MyOfferedItems = new TradeStatusUser();
                TheirOfferedItems = new TradeStatusUser();
                foreach (var asset in myItems)
                {
                    MyOfferedItems.AddItem(asset);
                }
                foreach (var asset in theirItems)
                {
                    TheirOfferedItems.AddItem(asset);
                }
            }

            //checks if version needs to be updated
            private bool ShouldUpdate(bool check)
            {
                if (check)
                {
                    NewVersion = true;
                    Version++;
                    return true;
                }
                return false;
            }

            public bool AddMyItem(int appId, long contextId, long assetId, long amount = 1)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateItemAsset(appId, contextId, assetId, amount);
                return ShouldUpdate(MyOfferedItems.AddItem(asset));
            }

            public bool AddTheirItem(int appId, long contextId, long assetId, long amount = 1)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateItemAsset(appId, contextId, assetId, amount);
                return ShouldUpdate(TheirOfferedItems.AddItem(asset));
            }

            public bool AddMyCurrencyItem(int appId, long contextId, long currencyId, long amount)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateCurrencyAsset(appId, contextId, currencyId, amount);
                return ShouldUpdate(MyOfferedItems.AddCurrencyItem(asset));
            }

            public bool AddTheirCurrencyItem(int appId, long contextId, long currencyId, long amount)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateCurrencyAsset(appId, contextId, currencyId, amount);
                return ShouldUpdate(TheirOfferedItems.AddCurrencyItem(asset));
            }

            public bool RemoveMyItem(int appId, long contextId, long assetId, long amount = 1)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateItemAsset(appId, contextId, assetId, amount);
                return ShouldUpdate(MyOfferedItems.RemoveItem(asset));
            }

            public bool RemoveTheirItem(int appId, long contextId, long assetId, long amount = 1)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateItemAsset(appId, contextId, assetId, amount);
                return ShouldUpdate(TheirOfferedItems.RemoveItem(asset));
            }

            public bool RemoveMyCurrencyItem(int appId, long contextId, long currencyId, long amount)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateCurrencyAsset(appId, contextId, currencyId, amount);
                return ShouldUpdate(MyOfferedItems.RemoveCurrencyItem(asset));
            }

            public bool RemoveTheirCurrencyItem(int appId, long contextId, long currencyId, long amount)
            {
                var asset = new TradeStatusUser.TradeAsset();
                asset.CreateCurrencyAsset(appId, contextId, currencyId, amount);
                return ShouldUpdate(TheirOfferedItems.RemoveCurrencyItem(asset));
            }

            public bool TryGetMyItem(int appId, long contextId, long assetId, long amount, out TradeStatusUser.TradeAsset asset)
            {
                var tradeAsset = new TradeStatusUser.TradeAsset()
                {
                    AppId = appId,
                    Amount = amount,
                    AssetId = assetId,
                    ContextId = contextId
                };
                asset = new TradeStatusUser.TradeAsset();
                foreach (var item in MyOfferedItems.Assets)
                {
                    if (item.Equals(tradeAsset))
                    {
                        asset = item;
                        return true;
                    }
                }
                return false;
            }

            public bool TryGetTheirItem(int appId, long contextId, long assetId, long amount, out TradeStatusUser.TradeAsset asset)
            {
                var tradeAsset = new TradeStatusUser.TradeAsset()
                {
                    AppId = appId,
                    Amount = amount,
                    AssetId = assetId,
                    ContextId = contextId
                };
                asset = new TradeStatusUser.TradeAsset();
                foreach (var item in TheirOfferedItems.Assets)
                {
                    if (item.Equals(tradeAsset))
                    {
                        asset = item;
                        return true;
                    }
                }
                return false;
            }

            public bool TryGetMyCurrencyItem(int appId, long contextId, long currencyId, long amount, out TradeStatusUser.TradeAsset asset)
            {
                var tradeAsset = new TradeStatusUser.TradeAsset()
                {
                    AppId = appId,
                    Amount = amount,
                    CurrencyId = currencyId,
                    ContextId = contextId
                };
                asset = new TradeStatusUser.TradeAsset();
                foreach (var item in MyOfferedItems.Currency)
                {
                    if (item.Equals(tradeAsset))
                    {
                        asset = item;
                        return true;
                    }
                }
                return false;
            }

            public bool TryGetTheirCurrencyItem(int appId, long contextId, long currencyId, long amount, out TradeStatusUser.TradeAsset asset)
            {
                var tradeAsset = new TradeStatusUser.TradeAsset()
                {
                    AppId = appId,
                    Amount = amount,
                    CurrencyId = currencyId,
                    ContextId = contextId
                };
                asset = new TradeStatusUser.TradeAsset();
                foreach (var item in TheirOfferedItems.Currency)
                {
                    if (item.Equals(tradeAsset))
                    {
                        asset = item;
                        return true;
                    }
                }
                return false;
            }

            public List<TradeStatusUser.TradeAsset> GetMyItems()
            {
                return MyOfferedItems.Assets;
            }

            public List<TradeStatusUser.TradeAsset> GetTheirItems()
            {
                return TheirOfferedItems.Assets;
            }
        }

        public class TradeAssetsConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                List<TradeStatusUser.TradeAsset> assetList = ((Dictionary<TradeStatusUser.TradeAsset, TradeStatusUser.TradeAsset>)value).Select(x => x.Value).ToList();
                serializer.Serialize(writer, assetList);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                List<TradeStatusUser.TradeAsset> assets = serializer.Deserialize<List<TradeStatusUser.TradeAsset>>(reader);
                return assets.ToDictionary(x => x, x => x);
            }

            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(Dictionary<TradeStatusUser.TradeAsset, TradeStatusUser.TradeAsset>) || objectType == typeof(List<TradeStatusUser.TradeAsset>));
            }
        }

        public class TradeStatusUser
        {
            [JsonProperty("assets")]
            public List<TradeAsset> Assets { get; set; }

            [JsonProperty("currency")]
            public List<TradeAsset> Currency { get; set; }

            [JsonProperty("ready")]
            public bool IsReady { get; set; }

            public TradeStatusUser()
            {
                Assets = new List<TradeAsset>();
                IsReady = false;
                Currency = new List<TradeAsset>();
            }

            internal bool AddItem(TradeAsset asset)
            {
                if (!Assets.Contains(asset))
                {
                    Assets.Add(asset);
                    return true;
                }
                return false;
            }

            internal bool AddCurrencyItem(TradeAsset asset)
            {
                if (!Currency.Contains(asset))
                {
                    Currency.Add(asset);
                    return true;
                }
                return false;
            }

            internal bool RemoveItem(TradeAsset asset)
            {
                return Assets.Contains(asset) && Assets.Remove(asset);
            }

            internal bool RemoveCurrencyItem(TradeAsset asset)
            {
                return Currency.Contains(asset) && Currency.Remove(asset);
            }

            public bool ContainsItem(TradeAsset asset)
            {
                return Assets.Contains(asset);
            }

            public class ValueStringConverter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    writer.WriteValue(value.ToString());
                    writer.Flush();
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                {
                    throw new NotImplementedException();
                }

                public override bool CanConvert(Type objectType)
                {
                    return true;
                }
            }
            public class TradeAsset : IEquatable<TradeAsset>
            {
                [JsonProperty("appid")]
                public long AppId { get; set; }

                [JsonProperty("contextid")]
                public long ContextId { get; set; }

                [JsonProperty("amount")]
                public long Amount { get; set; }

                [JsonProperty("assetid"), JsonConverter(typeof(ValueStringConverter))]
                public long AssetId { get; set; }

                [JsonProperty("currencyid"), JsonConverter(typeof(ValueStringConverter))]
                public long CurrencyId { get; set; }

                public void CreateItemAsset(long appId, long contextId, long assetId, long amount)
                {
                    this.AppId = appId;
                    this.ContextId = contextId;
                    this.AssetId = assetId;
                    this.Amount = amount;
                    this.CurrencyId = 0;
                }

                public void CreateCurrencyAsset(long appId, long contextId, long currencyId, long amount)
                {
                    this.AppId = appId;
                    this.ContextId = contextId;
                    this.CurrencyId = currencyId;
                    this.Amount = amount;
                    this.AssetId = 0;
                }

                public bool ShouldSerializeAssetId()
                {
                    return AssetId != 0;
                }

                public bool ShouldSerializeCurrencyId()
                {
                    return CurrencyId != 0;
                }

                public bool Equals(TradeAsset other)
                {
                    if (this.AppId == other.AppId && this.ContextId == other.ContextId &&
                        this.CurrencyId == other.CurrencyId && this.AssetId == other.AssetId &&
                        this.Amount == other.Amount)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
