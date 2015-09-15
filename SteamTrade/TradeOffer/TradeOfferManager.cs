using System.Collections.Generic;
using System.Diagnostics;
using SteamKit2;
using System;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferManager
    {
        private readonly HashSet<string> tradeOfferHistory = new HashSet<string>();
        private readonly OfferSession session;
        private readonly TradeOfferWebAPI webApi;

        public int LastTimeCheckedOffers { get; private set; }

        public TradeOfferManager(string apiKey, SteamWeb steamWeb)
        {
            if (apiKey == null)
                throw new ArgumentNullException("apiKey");

            webApi = new TradeOfferWebAPI(apiKey, steamWeb);
            session = new OfferSession(webApi, steamWeb);
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
            var offers = webApi.GetActiveTradeOffers(false, true, false);
            if (offers != null && offers.TradeOffersReceived != null)
            {
                foreach (var offer in offers.TradeOffersReceived)
                {
                    if (offer.TradeOfferState == TradeOfferState.TradeOfferStateActive && !tradeOfferHistory.Contains(offer.TradeOfferId))
                    {
                        //make sure the api loaded correctly sometimes the items are missing
                        if (IsOfferValid(offer))
                        {
                            SendOfferToHandler(offer);
                        }
                        else
                        {
                            var resp = webApi.GetTradeOffer(offer.TradeOfferId);
                            if (IsOfferValid(resp.Offer))
                            {
                                SendOfferToHandler(resp.Offer);
                            }
                            else
                            {
                                //todo: log steam api is giving us invalid offers.
                                Debug.WriteLine("Offer returned from steam api is not valid : " + resp.Offer.TradeOfferId);
                            }
                        }
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
                        //make sure the api loaded correctly sometimes the items are missing
                        if (IsOfferValid(offer))
                        {
                            SendOfferToHandler(offer);
                        }
                        else
                        {
                            var resp = webApi.GetTradeOffer(offer.TradeOfferId);
                            if (IsOfferValid(resp.Offer))
                            {
                                SendOfferToHandler(resp.Offer);
                            }
                            else
                            {
                                //todo: log steam api is giving us invalid offers.
                                Debug.WriteLine("Offer returned from steam api is not valid : " + resp.Offer.TradeOfferId);
                            }
                        }
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

        private bool IsOfferValid(Offer offer)
        {
            if ((offer.ItemsToGive != null && offer.ItemsToGive.Count != 0) 
                || (offer.ItemsToReceive != null && offer.ItemsToReceive.Count != 0))
            {
                return true;
            }
            return false;
        }

        private void SendOfferToHandler(Offer offer)
        {
            var tradeOffer = new TradeOffer(session, offer);
            tradeOfferHistory.Add(offer.TradeOfferId);
            OnNewTradeOffer(tradeOffer);
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
                    //todo: log steam api is giving us invalid offers.
                    Debug.WriteLine("Offer returned from steam api is not valid : " + resp.Offer.TradeOfferId);
                }
            }
            return false;
        }
    }
}