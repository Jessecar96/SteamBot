using SteamKit2;
using SteamAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamBot
{
    public class TradeOfferUserHandler : UserHandler
    {
        public TradeOfferUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnTradeOfferReceived(TradeOffers.TradeOffer tradeOffer)
        {
            if (IsAdmin)
            {
                TradeOffers.AcceptTrade(tradeOffer.Id);
            }
            else
            {
                TradeOffers.DeclineTrade(tradeOffer.Id);
            }
        }

        public override void OnTradeOfferAccepted(TradeOffers.TradeOffer tradeOffer)
        {
            var tradeOfferId = tradeOffer.Id;
            var myItems = tradeOffer.ItemsToGive;
            var userItems = tradeOffer.ItemsToReceive;

            Log.Info("Trade offer #{0} accepted. Items to give: {1}, Items to receive: {2}", tradeOfferId, myItems.Length, userItems.Length);

            // myItems is now in user inventory
            // userItems is now in bot inventory
        }

        public override void OnTradeOfferDeclined(TradeOffers.TradeOffer tradeOffer)
        {
            Log.Warn("Trade offer #{0} has been declined.", tradeOffer.Id);
        }

        public override void OnTradeOfferInvalid(TradeOffers.TradeOffer tradeOffer)
        {
            Log.Warn("Trade offer #{0} is invalid, with state: {1}.", tradeOffer.Id, tradeOffer.State);
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            if (IsAdmin)
            {
                //creating a new trade offer
                var tradeOffer = TradeOffers.CreateTrade(OtherSID);

                //tradeOffer.AddMyItem(0, 0, 0);

                var tradeOfferId = tradeOffer.SendTrade("message");
                if (tradeOfferId > 0)
                {
                    Log.Success("Trade offer sent : Offer ID " + tradeOfferId);
                }

                // sending trade offer with token
                // "token" should be replaced with the actual token from the other user
                var tradeOfferIdWithToken = tradeOffer.SendTradeWithToken("message", "token");
                if (tradeOfferIdWithToken > 0)
                {
                    Log.Success("Trade offer sent : Offer ID " + tradeOfferIdWithToken);
                }
            }
        }

        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() { }

        private bool DummyValidation(List<TradeOffers.TradeOffer.CEconAsset> myAssets, List<TradeOffers.TradeOffer.CEconAsset> theirAssets)
        {
            //compare items etc
            if (myAssets.Count == theirAssets.Count)
            {
                return true;
            }
            return false;
        }
    }
}
