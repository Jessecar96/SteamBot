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

			Log.Info ("Trade offer recieved from user ID: " + offer.PartnerSteamId);

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
				if (item.AppId != 570) {
					if (offer.Decline ()) {
						Log.Error ("Offer declined because one or more items was not for CS: GO.");
					}
					return;
				}
			}

			Log.Info ("Offer is a valid deposit, accepting offer.");
			string tradeid;
			if (offer.Accept (out tradeid)) {
				Log.Success ("Trade offer accepted, sending data to server. Trade ID: " + tradeid);

				//Send request to server with items and steam ID of user
				var request = (HttpWebRequest)WebRequest.Create ("http://csgowinbig.jordanturley.com/php/deposit.php");

				var postData = "owner=" + offer.PartnerSteamId;
				postData += "&allItems=";

				ArrayList allItems = new ArrayList();

				foreach (TradeAsset item in theirItems) {
					string name = item.ToString ();

					var itemPriceRequest = (HttpWebRequest)WebRequest.Create("http://www.example.com/recepticle.aspx");
					var itemPriceResponse = (HttpWebResponse)itemPriceRequest.GetResponse();
					var itemPriceResponseString = new StreamReader(itemPriceResponse.GetResponseStream()).ReadToEnd();

					JObject j = JObject.Parse (@itemPriceResponseString);
					string itemValueStr = (string)j["median_price"];
					itemValueStr = itemValueStr.Substring (4);

					int itemValue = Convert.ToInt32 (itemValueStr, 10) * 100;

					CSGOItem itemObj = new CSGOItem();
					itemObj.name = name;
					itemObj.price = itemValue;

					allItems.Add (itemObj);
				}

				string allItemsJson = JsonConvert.SerializeObject (allItems);
				postData += allItemsJson;

				var data = Encoding.ASCII.GetBytes(postData);

				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = data.Length;

				using (var stream = request.GetRequestStream()) {
					stream.Write(data, 0, data.Length);
				}

				var response = (HttpWebResponse)request.GetResponse();

				var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

				//Check responseString to see if it is the end of a round
				//If it is, send out the trade to the winner, and send offer to the profit account, with ~2% of items
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
		public string name;
		public int price;
	}
}
