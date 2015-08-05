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
			//Get password from file on desktop
			string pass = System.IO.File.ReadAllText(@"C:\Users\Jordan\Desktop\password.txt");

			//Get items in the trade, and ID of user sending trade
			var theirItems = offer.Items.GetTheirItems ();
			var myItems = offer.Items.GetMyItems ();
			var userID = offer.PartnerSteamId;

			bool shouldDecline = false;

			//Check if they are trying to get items from the bot
			if (myItems.Count > 0 || theirItems.Count == 0) {
				shouldDecline = true;
				Log.Error ("Offer declined because the offer wasn't a gift; the user wanted items instead of giving.");
			}

			//Check to make sure all items are for CS: GO.
			foreach (TradeAsset item in theirItems) {
				if (item.AppId != 730) {
					shouldDecline = true;
					Log.Error ("Offer declined because one or more items was not for CS: GO.");
				}
			}

			if (shouldDecline) {
				if (offer.Decline ()) {
					Log.Error ("Offer declined.");
				}
				return;
			}

			Log.Success ("Offer approved, accepting.");

			//Send items to server and check if all items add up to more than $1.
			//If they do, accept the trade. If they don't, decline the trade.
			string postData = "password=" + pass;
			postData += "&owner=" + userID;

			string theirItemsJSON = JsonConvert.SerializeObject (theirItems);

			postData += "&items=" + theirItemsJSON;

			Log.Success ("Post data string: \n" + postData);

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

			Log.Success ("Response received from server: \n" + responseString);

			JSONClass responseJsonObj = JsonConvert.DeserializeObject<JSONClass> (responseString);

			if (responseJsonObj.success == 1) {
				//Get data array from json
				var jsonData = responseJsonObj.data;

				if (jsonData.minDeposit == 1) {
					if (offer.Accept ()) {
						Log.Success ("Offer accepted from " + userID);
					}

					//Check if the pot is over
					if (jsonData.potOver == 1) {
						//Get the winner, and items to give and keep

						var itemsToGive = jsonData.tradeItems;
						var itemsToKeep = jsonData.profitItems;

						string winnerSteamIDString = jsonData.winnerSteamID;
						SteamID winnerSteamID = new SteamID (winnerSteamIDString);

						//Create trade offer for the winner
						var tradeOffer = Bot.NewTradeOffer (winnerSteamID);

						//Loop through all winner's items and add them to trade
						foreach (CSGOItem item in itemsToGive) {
							
						}
					}
				} else {
					if (offer.Decline ()) {
						Log.Error ("Minimum deposit not reached, offer declined.");
					}
				}
			} else {
				Log.Error ("Server deposit request failed, declining trade. Error message:\n" + responseJsonObj.errMsg);
				offer.Decline ();
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

	public class JSONClass {
		public int success;
		public string errMsg; //Error message
		public Data data;
	}

	public class Data {
		public int minDeposit;
		public int potOver;
		public string winnerSteamID;
		public List<CSGOItem> tradeItems;
		public List<CSGOItem> profitItems;
	}

	public class CSGOItemFromWeb {
		public long appId;
		public long contextId;
		public long assetId;

	}
}