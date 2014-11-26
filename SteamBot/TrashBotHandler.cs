using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

namespace SteamBot
{
    /// <summary>
    /// A basic handler to replicate the functionality of the
    /// node-steam-trash-bot (github.com/bonnici/node-steam-trash-bot)
    /// Adapted from TradeOfferUserHandler
    /// </summary>
    public class TrashBotHandler : UserHandler
    {
        public TrashBotHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnNewTradeOffer(TradeOffer offer)
        {
            //receiving a trade offer 
            // todo: accept trade requests from anybody on friends list
            if (IsAdmin)
            {
                //parse inventories of bot and other partner
                //either with webapi or generic inventory
                //Bot.GetInventory();
                //Bot.GetOtherInventory(OtherSID);

                var myItems = offer.Items.GetMyItems();
                var theirItems = offer.Items.GetTheirItems();
                Log.Info("They want " + myItems.Count + " of my items.");
                Log.Info("And I will get " +  theirItems.Count + " of their items.");

                // accept all trade requests - trash bot
                string tradeid;
                if (offer.Accept(out tradeid))
                {
                    Log.Success("Accepted trade offer successfully : Trade ID: " + tradeid);
                }
            }
            else
            {
                // we don't know this user so we can decline
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

                //creating a new trade offer with token
                var offerWithToken = Bot.NewTradeOffer(OtherSID);

                //offer.Items.AddMyItem(0, 0, 0);
                if (offerWithToken.Items.NewVersion)
                {
                    string newOfferId;
                    // "token" should be replaced with the actual token from the other user
                    if (offerWithToken.SendWithToken(out newOfferId, "token"))
                    {
                        Log.Success("Trade offer sent : Offer ID " + newOfferId);
                    }
                }
            }
        }

        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { 
            // todo: some kind of remote friends list admin
            return IsAdmin;
        }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() {
/*            string logonCompleteMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - I have arrived.";
            Bot.SteamFriends.SendChatMessage(AdminID,
                                      EChatEntryType.ChatMsg,
                                      logonCompleteMessage
                                      );
            */
 }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeError(string error) {
            Bot.SteamFriends.SendChatMessage(OtherSID,
                                       EChatEntryType.ChatMsg,
                                       "Oh, there was an error: " + error + "."
                                       );
            Bot.log.Warn(error);
        }

        public override void OnTradeTimeout() {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                      "Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info("User was kicked because he was AFK.");
        }

        public override void OnTradeSuccess() {
            // Trade completed successfully
            Log.Success("Trade Complete.");
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                      "Trade Complete.");

        }

        public override void OnTradeInit() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            Trade.Poll();
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                if (IsAdmin)
                {
                    Trade.SetReady(true);
                }
            }
        }

        public override void OnTradeAccept() {
            if (IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try
                {
                    Trade.AcceptTrade();
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
                }

                Log.Success("Trade Complete!");
            }
        }

        /*private bool DummyValidation(List<TradeAsset> myAssets, List<TradeAsset> theirAssets)
        {
            //compare items etc
            if (myAssets.Count == theirAssets.Count)
            {
                return true;
            }
            return false
        }
        */
    }
}
