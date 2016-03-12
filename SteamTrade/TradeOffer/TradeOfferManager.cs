using System.Collections.Generic;
using System.Diagnostics;
using SteamKit2;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferManager
    {
        private readonly Dictionary<string, TradeOfferState> knownTradeOffers = new Dictionary<string, TradeOfferState>();
        private readonly OfferSession session;
        private readonly TradeOfferWebAPI webApi;
        private readonly Queue<Offer> unhandledTradeOfferUpdates; 

        public DateTime LastTimeCheckedOffers { get; private set; }

        public TradeOfferManager(string apiKey, SteamWeb steamWeb)
        {
            if (apiKey == null)
                throw new ArgumentNullException("apiKey");

            LastTimeCheckedOffers = DateTime.MinValue;
            webApi = new TradeOfferWebAPI(apiKey, steamWeb);
            session = new OfferSession(webApi, steamWeb);
            unhandledTradeOfferUpdates = new Queue<Offer>();
        }

        public delegate void TradeOfferUpdatedHandler(TradeOffer offer);

        /// <summary>
        /// Occurs when a new trade offer has been made by the other user
        /// </summary>
        public event TradeOfferUpdatedHandler OnTradeOfferUpdated;

        public void EnqueueUpdatedOffers()
        {
            DateTime startTime = DateTime.Now;

            var offersResponse = (LastTimeCheckedOffers == DateTime.MinValue
                ? webApi.GetAllTradeOffers()
                : webApi.GetAllTradeOffers(GetUnixTimeStamp(LastTimeCheckedOffers).ToString()));
            AddTradeOffersToQueue(offersResponse);

            LastTimeCheckedOffers = startTime - TimeSpan.FromMinutes(5); //Lazy way to make sure we don't miss any trade offers due to slightly differing clocks
        }

        private void AddTradeOffersToQueue(OffersResponse offers)
        {
            if (offers == null || offers.AllOffers == null)
                return;

            lock(unhandledTradeOfferUpdates)
            {
                foreach(var offer in offers.AllOffers)
                {
                    unhandledTradeOfferUpdates.Enqueue(offer);
                }
            }
        }

        public bool HandleNextPendingTradeOfferUpdate()
        {
            Offer nextOffer;
            lock (unhandledTradeOfferUpdates)
            {
                if (!unhandledTradeOfferUpdates.Any())
                {
                    return false;
                }
                nextOffer = unhandledTradeOfferUpdates.Dequeue();
            }

            return HandleTradeOfferUpdate(nextOffer);
        }

        private bool HandleTradeOfferUpdate(Offer offer)
        {
            if(knownTradeOffers.ContainsKey(offer.TradeOfferId) && knownTradeOffers[offer.TradeOfferId] == offer.TradeOfferState)
            {
                return false;
            }

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
                    return false;
                }
            }
            return true;
        }

        private bool IsOfferValid(Offer offer)
        {
            bool hasItemsToGive = offer.ItemsToGive != null && offer.ItemsToGive.Count != 0;
            bool hasItemsToReceive = offer.ItemsToReceive != null && offer.ItemsToReceive.Count != 0;
            return hasItemsToGive || hasItemsToReceive;
        }

        private void SendOfferToHandler(Offer offer)
        {
            knownTradeOffers[offer.TradeOfferId] = offer.TradeOfferState;
            OnTradeOfferUpdated(new TradeOffer(session, offer));
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

        public bool TryGetOffer(string offerId, out TradeOffer tradeOffer)
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