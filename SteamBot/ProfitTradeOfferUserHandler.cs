using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

using System.IO;
using System.Collections;
using System.Text;
using System.Net;
using System.Web.Script.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SteamBot
{
    public class ProfitTradeOfferUserHandler : UserHandler
    {
        public ProfitTradeOfferUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnNewTradeOffer(TradeOffer offer)
        {
			var theirItems = offer.Items.GetTheirItems ();
			var myItems = offer.Items.GetMyItems ();

			if (myItems.Count > 0 || theirItems.Count == 0) { //Only accept gifts
				if (offer.Decline ()) {
					Log.Error ("Profit offer declined, myItems > 0 or theirItems = 0.");
				}
				return;
			}

			//Check to make sure the offer is from the deposit account
			//Also check counts again just to be sure
			if (offer.PartnerSteamId.ConvertToUInt64 () == 76561198238743988 && myItems.Count == 0 && theirItems.Count > 0) {
				if (offer.Accept ()) {
					Log.Success ("Profit offer accepted!");
				}
			} else {
				if (offer.Decline ()) {
					Log.Error ("Trade offer received from unknown user.");
				}
			}
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
			SendChatMessage (Bot.ChatResponse);
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
    }
}
