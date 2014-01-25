using System.Collections.Generic;
using SteamKit2;
using System;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferManager
    {
        HashSet<string> tradeOfferHistory = new HashSet<string>();
        public int LastTimeCheckedOffers { get; private set; }

        private string apiKey;
        private string sessionId;
        private string token;

        private OfferSession session;
        private TradeOfferWebAPI webApi;

        public TradeOfferManager(string apiKey, string sessionId, string token)
        {
            if (apiKey == null)
                throw new ArgumentNullException("apiKey");

            if (sessionId == null)
                throw new ArgumentNullException("sessionId");

            if (token == null)
                throw new ArgumentNullException("token");

            this.apiKey = apiKey;
            this.sessionId = sessionId;
            this.token = token;
            this.session = new OfferSession(this.sessionId, this.token);

            webApi = new TradeOfferWebAPI(apiKey);
        }

        public delegate void NewOfferHandler(TradeOffer offer);

        /// <summary>
        /// Occurs when a new trade offer has been made by the other user
        /// </summary>
        public event NewOfferHandler OnNewTradeOffer;

        /// <summary>
        /// Gets the currently active trade offers from the web api and processes them
        /// </summary>
        /// <returns></returns>
        public bool GetActiveTradeOffers()
        {
            var offers = webApi.GetTradeOffers(false, true, false, true, false);
            if (offers != null && offers.TradeOffersReceived != null)
            {
                foreach (var offer in offers.TradeOffersReceived)
                {
                    if (offer.TradeOfferState == TradeOfferState.TradeOfferStateActive && !tradeOfferHistory.Contains(offer.TradeOfferId))
                    {
                        var tradeOffer = new TradeOffer(session, offer);
                        tradeOfferHistory.Add(offer.TradeOfferId);
                        OnNewTradeOffer(tradeOffer);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get any updated offers
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public bool GetTradeOffersSince(int unixTimeStamp)
        {
            var offers = webApi.GetTradeOffers(false, true, false, true, false, unixTimeStamp.ToString());
            if (offers != null && offers.TradeOffersReceived != null)
            {
                foreach (var offer in offers.TradeOffersReceived)
                {
                    if (offer.TradeOfferState == TradeOfferState.TradeOfferStateActive && !tradeOfferHistory.Contains(offer.TradeOfferId))
                    {
                        var tradeOffer = new TradeOffer(session, offer);
                        tradeOfferHistory.Add(offer.TradeOfferId);
                        OnNewTradeOffer(tradeOffer);
                    }
                }
                return true;
            }
            return false;
        }

        public bool GetOffers()
        {
            if (LastTimeCheckedOffers != 0)
            {
                bool action = GetTradeOffersSince(LastTimeCheckedOffers);

                if (action)
                    LastTimeCheckedOffers = GetUnixTimeStamp();
                return action;
            }
            else
            {
                bool action = GetActiveTradeOffers();
                if (action)
                    LastTimeCheckedOffers = GetUnixTimeStamp();
                return action;
            }
        }

        private int GetUnixTimeStamp()
        {
            return (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public TradeOffer NewOffer(SteamID other)
        {
            var offer = new TradeOffer(session, other);
            return offer;
        }

        public bool GetOffer(string offerId, out TradeOffer tradeOffer)
        {
            tradeOffer = null;
            var resp = webApi.GetTradeOffer(Convert.ToUInt64(offerId));
            if (resp != null)
            {
                tradeOffer = new TradeOffer(session, resp.Offer);
                return true;
            }
            return false;
        }
    }
}