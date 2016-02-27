using System.Collections.Generic;
using System.Diagnostics;
using SteamKit2;
using System;
using System.Linq;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferManager
    {
        private readonly Dictionary<string, TradeOfferState> knownTradeOffers = new Dictionary<string, TradeOfferState>();
        private readonly OfferSession session;
        private readonly TradeOfferWebAPI webApi;

        public DateTime LastTimeCheckedOffers { get; private set; }

        public TradeOfferManager(string apiKey, SteamWeb steamWeb)
        {
            if (apiKey == null)
                throw new ArgumentNullException("apiKey");

            LastTimeCheckedOffers = DateTime.MinValue;
            webApi = new TradeOfferWebAPI(apiKey, steamWeb);
            session = new OfferSession(webApi, steamWeb);
        }

        public delegate void TradeOfferUpdatedHandler(TradeOffer offer);

        /// <summary>
        /// Occurs when a new trade offer has been made by the other user
        /// </summary>
        public event TradeOfferUpdatedHandler OnTradeOfferUpdated;

        public bool GetAllTradeOffers()
        {
            var offers = webApi.GetAllTradeOffers();
            return HandleTradeOffersResponse(offers);
        }

        /// <summary>
        /// Gets the currently active trade offers from the web api and processes them
        /// </summary>
        public bool GetActiveTradeOffers()
        {
            var offers = webApi.GetActiveTradeOffers(false, true, false);
            return HandleTradeOffersResponse(offers);
        }

        /// <summary>
        /// Get any updated offers
        /// </summary>
        public bool GetTradeOffersSince(DateTime dateTime)
        {
            var offers = webApi.GetAllTradeOffers(GetUnixTimeStamp(dateTime).ToString());
            return HandleTradeOffersResponse(offers);
        }

        private bool HandleTradeOffersResponse(OffersResponse offers)
        {
            if (offers?.AllOffers == null)
                return false;

            foreach(var offer in offers.AllOffers)
            {
                if(!knownTradeOffers.ContainsKey(offer.TradeOfferId) || knownTradeOffers[offer.TradeOfferId] != offer.TradeOfferState)
                {
                    //make sure the api loaded correctly sometimes the items are missing
                    if(IsOfferValid(offer))
                    {
                        SendOfferToHandler(offer);
                    }
                    else
                    {
                        var resp = webApi.GetTradeOffer(offer.TradeOfferId);
                        if(IsOfferValid(resp.Offer))
                        {
                            SendOfferToHandler(resp.Offer);
                        }
                        else
                        {
                            Debug.WriteLine("Offer returned from steam api is not valid : " + resp.Offer.TradeOfferId);
                        }
                    }
                }
            }
            return true;
        }

        public bool GetOffers()
        {
            bool offersChecked = (LastTimeCheckedOffers == DateTime.MinValue ? GetAllTradeOffers() : GetTradeOffersSince(LastTimeCheckedOffers));

            if(offersChecked)
                LastTimeCheckedOffers = DateTime.Now;

            return offersChecked;
        }

        private bool IsOfferValid(Offer offer)
        {
            bool hasItemsToGive = offer.ItemsToGive != null && offer.ItemsToGive.Count != 0;
            bool hasItemsToReceive = offer.ItemsToReceive != null && offer.ItemsToReceive.Count != 0;
            return hasItemsToGive || hasItemsToReceive;
        }

        private void SendOfferToHandler(Offer offer)
        {
            var tradeOffer = new TradeOffer(session, offer);
            knownTradeOffers[offer.TradeOfferId] = offer.TradeOfferState;
            OnTradeOfferUpdated(tradeOffer);
        }

        private uint GetUnixTimeStamp(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (uint)((dateTime.ToUniversalTime() - epoch).TotalSeconds);
        }

        public TradeOffer NewOffer(SteamID other)
        {
            return new TradeOffer(session, other);
        }

        public bool GetOffer(string offerId, out TradeOffer tradeOffer)
        {
            tradeOffer = null;
            var resp = webApi.GetTradeOffer(offerId);
            if (resp != null)
            {
                if (IsOfferValid(resp.Offer))
                {
                    tradeOffer = new TradeOffer(session, resp.Offer);
                    return true;
                }
                else
                {
                    Debug.WriteLine("Offer returned from steam api is not valid : " + resp.Offer.TradeOfferId);
                }
            }
            return false;
        }
    }
}