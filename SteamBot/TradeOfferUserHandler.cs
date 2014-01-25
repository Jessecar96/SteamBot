using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

namespace SteamBot
{
    public class TradeOfferUserHandler : UserHandler
    {
        public TradeOfferUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnNewTradeOffer(TradeOffer offer)
        {
            //receiving a trade offer 
            if (IsAdmin)
            {
                //parse inventories of bot and other partner
                //either with webapi or generic inventory
                Bot.GetInventory();
                Bot.GetOtherInventory(OtherSID);

                var myItems = offer.Items.GetMyItems();
                var theirItems = offer.Items.GetTheirItems();
                Console.WriteLine("They want {0} of my items", myItems.Count);
                Console.WriteLine("And I will get {0} of their items", theirItems.Count);

                //do validation logic etc
                if (DummyValidation(myItems, theirItems))
                {
                    string tradeid;
                    if (offer.Accept(out tradeid))
                    {
                        Log.Success("Accepted trade offer successfully : Trade ID: " + tradeid);
                    }
                }
                else
                {
                    // maybe we want different items or something

                    //offer.Items.AddMyItem(0, 0, 0);
                    //offer.Items.RemoveTheirItem(0, 0, 0);
                    if (offer.Items.NewVersion)
                    {
                        string newOfferId;
                        if (offer.CounterOffer(out newOfferId))
                        {
                            Log.Success("Counter offered successfully : New Offer ID: " + newOfferId);
                        }
                    }
                }
            }
            else
            {
                //we don't know this user so we can decline
                if (offer.Decline())
                {
                    Log.Info("Declined trade offer : " + offer.TradeOfferId + " from untrusted user " + OtherSID.ConvertToUInt64());
                }
            }
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            if (IsAdmin)
            {
                //creating a new trade offer
                var offer = Bot.NewTradeOffer(OtherSID);

                //offer.Items.AddMyItem(0, 0, 0);
                if (offer.Items.NewVersion)
                {
                    string newOfferId;
                    if (offer.Send(out newOfferId))
                    {
                        Log.Success("Trade offer sent : Offer ID " + newOfferId);
                    }
                }
            }
        }

        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() { }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeError(string error) { }

        public override void OnTradeTimeout() { }

        public override void OnTradeSuccess() { }

        public override void OnTradeInit() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) { }

        public override void OnTradeAccept() { }

        private bool DummyValidation(Dictionary<TradeAsset, TradeAsset> myAssets, Dictionary<TradeAsset, TradeAsset> theirAssets)
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
