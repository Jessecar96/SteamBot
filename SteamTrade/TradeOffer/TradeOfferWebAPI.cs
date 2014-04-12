using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferWebAPI
    {
        public TradeOfferWebAPI(string apiKey)
        {
            if (apiKey == null)
            {
                throw new ArgumentNullException("apiKey");
            }
            ApiKey = apiKey;
        }

        private string ApiKey { get; set; }

        private const string BaseUrl = "http://api.steampowered.com/IEconService/{0}/{1}/{2}";

        public OfferResponse GetTradeOffer(string tradeofferid)
        {
            string options = string.Format("?key={0}&tradeofferid={1}&language={2}", ApiKey, tradeofferid, "en_us");
            string url = String.Format(BaseUrl, "GetTradeOffer", "v1", options);
            try
            {
                string response = SteamWeb.Fetch(url, "GET", null, null, false);
                var result = JsonConvert.DeserializeObject<ApiResponse<OfferResponse>>(response);
                return result.Response;
            }
            catch (Exception ex)
            {
                //todo log
                Debug.WriteLine(ex);
            }
            return new OfferResponse();
        }

        public TradeOfferState GetOfferState(string tradeofferid)
        {
            var resp = GetTradeOffer(tradeofferid);
            if (resp != null && resp.Offer != null)
            {
                return resp.Offer.TradeOfferState;
            }
            return TradeOfferState.TradeOfferStateUnknown;
        }

        public OffersResponse GetActiveTradeOffers(bool getSentOffers, bool getReceivedOffers, bool getDescriptions, string language = "en_us")
        {
            if (!getSentOffers && !getReceivedOffers)
            {
                throw new ArgumentException("getSentOffers and getReceivedOffers can't be both false");
            }

            string options = string.Format("?key={0}&get_sent_offers={1}&get_received_offers={2}&get_descriptions={3}&language={4}&active_only={5}",
                ApiKey, BoolConverter(getSentOffers), BoolConverter(getReceivedOffers), BoolConverter(getDescriptions), language, BoolConverter(true));
            string url = string.Format(BaseUrl, "GetTradeOffers", "v1", options);
            string response = SteamWeb.Fetch(url, "GET", null, null, false);
            try
            {
                var result = JsonConvert.DeserializeObject<ApiResponse<OffersResponse>>(response);
                return result.Response;
            }
            catch (Exception ex)
            {
                //todo log
                Debug.WriteLine(ex);
            }
            return new OffersResponse();
        }

        public OffersResponse GetTradeOffers(bool getSentOffers, bool getReceivedOffers, bool getDescriptions, bool activeOnly, bool historicalOnly, string timeHistoricalCutoff = "1389106496", string language = "en_us")
        {
            if (!getSentOffers && !getReceivedOffers)
            {
                throw new ArgumentException("getSentOffers and getReceivedOffers can't be both false");
            }

            string options = string.Format("?key={0}&get_sent_offers={1}&get_received_offers={2}&get_descriptions={3}&language={4}&active_only={5}&historical_only={6}&time_historical_cutoff={7}",
                ApiKey, BoolConverter(getSentOffers), BoolConverter(getReceivedOffers), BoolConverter(getDescriptions), language, BoolConverter(activeOnly), BoolConverter(historicalOnly), timeHistoricalCutoff);
            string url = String.Format(BaseUrl, "GetTradeOffers", "v1", options);
            string response = SteamWeb.Fetch(url, "GET", null, null, false);
            try
            {
                var result = JsonConvert.DeserializeObject<ApiResponse<OffersResponse>>(response);
                return result.Response;
            }
            catch (Exception ex)
            {
                //todo log
                Debug.WriteLine(ex);
            }
            return new OffersResponse();
        }

        private static string BoolConverter(bool value)
        {
            return value ? "1" : "0";
        }

        public TradeOffersSummary GetTradeOffersSummary(UInt32 timeLastVisit)
        {
            string options = string.Format("?key={0}&time_last_visit={1}", ApiKey, timeLastVisit);
            string url = String.Format(BaseUrl, "GetTradeOffersSummary", "v1", options);

            try
            {
                string response = SteamWeb.Fetch(url, "GET", null, null, false);
                var resp = JsonConvert.DeserializeObject<ApiResponse<TradeOffersSummary>>(response);

                return resp.Response;
            }
            catch (Exception ex)
            {
                //todo log
                Debug.WriteLine(ex);
            }
            return new TradeOffersSummary();
        }

        private bool DeclineTradeOffer(ulong tradeofferid)
        {
            string options = string.Format("?key={0}&tradeofferid={1}", ApiKey, tradeofferid);
            string url = String.Format(BaseUrl, "DeclineTradeOffer", "v1", options);
            Debug.WriteLine(url);
            string response = SteamWeb.Fetch(url, "POST", null, null, false);
            dynamic json = JsonConvert.DeserializeObject(response);

            if (json == null || json.success != "1")
            {
                return false;
            }
            return true;
        }

        private bool CancelTradeOffer(ulong tradeofferid)
        {
            string options = string.Format("?key={0}&tradeofferid={1}", ApiKey, tradeofferid);
            string url = String.Format(BaseUrl, "CancelTradeOffer", "v1", options);
            Debug.WriteLine(url);
            string response = SteamWeb.Fetch(url, "POST", null, null, false);
            dynamic json = JsonConvert.DeserializeObject(response);

            if (json == null || json.success != "1")
            {
                return false;
            }
            return true;
        }
    }

    public class TradeOffersSummary
    {
        [JsonProperty("pending_received_count")]
        public int PendingReceivedCount { get; set; }

        [JsonProperty("new_received_count")]
        public int NewReceivedCount { get; set; }

        [JsonProperty("updated_received_count")]
        public int UpdatedReceivedCount { get; set; }

        [JsonProperty("historical_received_count")]
        public int HistoricalReceivedCount { get; set; }

        [JsonProperty("pending_sent_count")]
        public int PendingSentCount { get; set; }

        [JsonProperty("newly_accepted_sent_count")]
        public int NewlyAcceptedSentCount { get; set; }

        [JsonProperty("updated_sent_count")]
        public int UpdatedSentCount { get; set; }

        [JsonProperty("historical_sent_count")]
        public int HistoricalSentCount { get; set; }
    }

    public class ApiResponse<T>
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }

    public class OfferResponse
    {
        [JsonProperty("offer")]
        public Offer Offer { get; set; }

        [JsonProperty("descriptions")]
        public List<AssetDescription> Descriptions { get; set; }
    }

    public class OffersResponse
    {
        [JsonProperty("trade_offers_sent")]
        public List<Offer> TradeOffersSent { get; set; }

        [JsonProperty("trade_offers_received")]
        public List<Offer> TradeOffersReceived { get; set; }

        [JsonProperty("descriptions")]
        public List<AssetDescription> Descriptions { get; set; }
    }

    public class Offer
    {
        [JsonProperty("tradeofferid")]
        public string TradeOfferId { get; set; }

        [JsonProperty("accountid_other")]
        public int AccountIdOther { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("expiration_time")]
        public int ExpirationTime { get; set; }

        [JsonProperty("trade_offer_state")]
        public TradeOfferState TradeOfferState { get; set; }

        [JsonProperty("items_to_give")]
        public List<CEconAsset> ItemsToGive { get; set; }

        [JsonProperty("items_to_receive")]
        public List<CEconAsset> ItemsToReceive { get; set; }

        [JsonProperty("is_our_offer")]
        public bool IsOurOffer { get; set; }

        [JsonProperty("time_created")]
        public int TimeCreated { get; set; }

        [JsonProperty("time_updated")]
        public int TimeUpdated { get; set; }
    }

    public enum TradeOfferState
    {
        TradeOfferStateInvalid = 1,
        TradeOfferStateActive = 2,
        TradeOfferStateAccepted = 3,
        TradeOfferStateCountered = 4,
        TradeOfferStateExpired = 5,
        TradeOfferStateCanceled = 6,
        TradeOfferStateDeclined = 7,
        TradeOfferStateInvalidItems = 8,
        TradeOfferStateUnknown
    }

    public class CEconAsset
    {
        [JsonProperty("appid")]
        public string AppId { get; set; }

        [JsonProperty("contextid")]
        public string ContextId { get; set; }

        [JsonProperty("assetid")]
        public string AssetId { get; set; }

        [JsonProperty("classid")]
        public string ClassId { get; set; }

        [JsonProperty("instanceid")]
        public string InstanceId { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("missing")]
        public bool IsMissing { get; set; }
    }

    public class AssetDescription
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("classid")]
        public string ClassId { get; set; }

        [JsonProperty("instanceid")]
        public string InstanceId { get; set; }

        [JsonProperty("currency")]
        public bool IsCurrency { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonProperty("descriptions")]
        public List<Description> Descriptions { get; set; }

        [JsonProperty("tradable")]
        public bool IsTradable { get; set; }

        [JsonProperty("owner_actions")]
        public List<OwnerAction> OwnerActions { get; set; }

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
    }

    public class Description
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class OwnerAction
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}