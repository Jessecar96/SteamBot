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
    public class DepositTradeOfferUserHandler : UserHandler
    {
        public DepositTradeOfferUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnNewTradeOffer(TradeOffer offer)
        {
			var botInfo = new Configuration.BotInfo();
			string pass = botInfo.Password;

			//Get items in the trade, and ID of user sending trade
			var theirItems = offer.Items.GetTheirItems ();
			var myItems = offer.Items.GetMyItems ();
			var userID = offer.PartnerSteamId;

			//Check if they are trying to get items from the bot
			if (myItems.Count > 0 || theirItems.Count == 0) {
				if (offer.Decline ()) {
					Log.Error ("Offer declined because the offer wasn't a gift; the user wanted items instead of giving.");
				}
				return;
			}

			//Check to make sure all items are for CS: GO.
			foreach (TradeAsset item in theirItems) {
				if (item.AppId != 730) {
					if (offer.Decline ()) {
						Log.Error ("Offer declined because one or more items was not for CS: GO.");
					}
					return;
				}
			}

			//Send items to server and check if all items add up to more than $1.
			//If they do, accept the trade. If they don't, decline the trade.
			string postData = "password=" + pass;
			postData += "&owner=" + userID;

			string theirItemsJSON = JsonConvert.SerializeObject (theirItems);
			postData += "&items=" + theirItemsJSON;

			string url = "http://csgowinbig.jordanturley.com/php/deposit.php";
			var request = (HttpWebRequest)WebRequest.Create (url);

			var data = Encoding.ASCII.GetBytes(postData);

			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = data.Length;

			using (var stream = request.GetRequestStream()) {
				stream.Write(data, 0, data.Length);
			}

			var response = (HttpWebResponse)request.GetResponse();

			var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

			var responseJsonObj = JObject.Parse (responseString);

			if (responseJsonObj ["success"] == 1) {
				if (responseJsonObj ["minDeposit"] == 1) {
					if (offer.Accept ()) {
						Log.Success ("Offer accepted from " + userID);
					}
				} else {
					if (offer.Decline ()) {
						Log.Error ("Minimum deposit not reached, offer declined.");
					}
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

	public class CSGOItem {
		public long appId;
		public long contextId;
		public long assetId;
	}
}
