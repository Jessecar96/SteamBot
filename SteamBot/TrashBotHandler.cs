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

        // trashbot accepts *all* trade offers
        // set your inventory privacy appropriately if you don't want the public taking all your items
        public override void OnNewTradeOffer(TradeOffer offer)
        {
            Log.Debug("[Trade ID " + offer.TradeOfferId + "] Inbound trade offer" );
            
            var theirItemCount = offer.Items.GetTheirItems().Count;
            var myItemCount = offer.Items.GetMyItems().Count;

            Log.Info("[Trade ID " + offer.TradeOfferId + "] Gaining " + theirItemCount + " item(s), losing " + myItemCount + " item(s)");

            if (offer.Accept())
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Trade Complete. [+" + theirItemCount + " -" + myItemCount + "]");
                Log.Success("[Trade ID " + offer.TradeOfferId + "] Accepted trade offer successfully");
            }
            else
                Log.Warn("[Trade ID " + offer.TradeOfferId + "] Trade offer failed");
        }

        public override void OnMessage(string message, EChatEntryType type) { }

        public override bool OnGroupAdd() { return false; }

        // todo: some kind of remote friends list admin
        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() { }

        public override bool OnTradeRequest() { return true; }

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
    }
}
