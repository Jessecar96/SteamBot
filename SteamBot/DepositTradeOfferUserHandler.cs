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

		public override void OnBotCommand(string command)
		{
			if (command.Equals ("withdraw")) {
				//Get current pot and all items in inventory
				string withdrawUrl = "http://csgowinbig.jordanturley.com/php/bot-withdraw.php";
				var withdrawRequest = (HttpWebRequest)WebRequest.Create (withdrawUrl);
				var withdrawResponse = (HttpWebResponse)withdrawRequest.GetResponse ();
				string withdrawString = new StreamReader (withdrawResponse.GetResponseStream()).ReadToEnd();

				WithdrawResponse botInventory = JsonConvert.DeserializeObject<WithdrawResponse> (withdrawString);

				var data = botInventory.data;

				var rgInventory = data.rgInventory;
				var currentPot = data.currentPot;

				var withdrawTradeOffer = Bot.NewTradeOffer (new SteamID(76561198020620333));

				foreach (var inventoryItemKeyVal in rgInventory) {
					var invItem = inventoryItemKeyVal.Value;
					long classId = invItem.classid, instanceId = invItem.instanceid;

					bool withdrawThisItem = true;
					//Check to see if this item is in the current pot
					foreach (var potItem in currentPot) {
						long classIdPot = potItem.classid, instanceIdPot = potItem.instanceid;

						if (classId == classIdPot && instanceId == instanceIdPot) {
							withdrawThisItem = false;
						}
					}

					if (withdrawThisItem) {
						var assetId = invItem.id;
						withdrawTradeOffer.Items.AddMyItem (730, 2, assetId, 1);
					}
				}

				if (withdrawTradeOffer.Items.GetMyItems ().Count != 0) {
					string withdrawOfferId;
					withdrawTradeOffer.Send (out withdrawOfferId, "Here are the withdraw items requested.");
					Log.Success ("Withdraw trade offer sent. Offer ID: " + withdrawOfferId);
				} else {
					Log.Error ("There are no profit items to withdraw at this time.");
				}
			}
		}

		public class WithdrawResponse {
			public int success;
			public string errMsg;
			public WithdrawData data;
		}

		public class WithdrawData {
			public IDictionary<string, inventoryItem> rgInventory;
			public List<inventoryItem> currentPot;
		}

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

			//Check if there are more than 10 items in the trade
			if (theirItems.Count > 10) {
				shouldDecline = true;
				Log.Error ("Offer declined because there were more than 10 items in the deposit.");
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

			//Log.Success ("Response received from server: \n" + responseString);

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
						//Get items to give and keep, and the winner and their trade token
						var itemsToGive = jsonData.tradeItems;
						var itemsToKeep = jsonData.profitItems;

						string winnerSteamIDString = jsonData.winnerSteamID;
						SteamID winnerSteamID = new SteamID (winnerSteamIDString);

						string winnerTradeToken = jsonData.winnerTradeToken;

						Log.Success ("Winner steam id: " + winnerSteamIDString + ", token: " + winnerTradeToken);

						//Get bot's inventory json
						string botInvUrl = "http://steamcommunity.com/profiles/76561198238743988/inventory/json/730/2";
						var botInvRequest = (HttpWebRequest)WebRequest.Create (botInvUrl);
						var botInvResponse = (HttpWebResponse)botInvRequest.GetResponse ();
						string botInvString = new StreamReader (botInvResponse.GetResponseStream()).ReadToEnd();

						BotInventory botInventory = JsonConvert.DeserializeObject<BotInventory> (botInvString);
						if (botInventory.success != true) {
							Log.Error ("An error occured while fetching the bot's inventory.");
							return;
						}
						var rgInventory = botInventory.rgInventory;

						//Create trade offer for the winner
						var winnerTradeOffer = Bot.NewTradeOffer (winnerSteamID);

						//Loop through all winner's items and add them to trade
						List<long> alreadyAddedToWinnerTrade = new List<long> ();
						foreach (CSGOItemFromWeb item in itemsToGive) {
							long classId = item.classId, instanceId = item.instanceId;

							//Loop through all inventory items and find the asset id for the item
							long assetId = 0;
							foreach (var inventoryItem in rgInventory) {
								var value = inventoryItem.Value;
								long tAssetId = value.id, tClassId = value.classid, tInstanceId = value.instanceid;

								if (tClassId == classId && tInstanceId == instanceId) {
									//Check if this assetId has already been added to the trade
									if (alreadyAddedToWinnerTrade.Contains (tAssetId)) {
										continue;
										//This is for when there are 2 of the same weapon, but they have different assetIds
									}
									assetId = tAssetId;
									break;
								}
							}

							//Log.Success ("Adding item to winner trade offer. Asset ID: " + assetId);

							winnerTradeOffer.Items.AddMyItem (730, 2, assetId, 1);
							alreadyAddedToWinnerTrade.Add (assetId);
						}

						//Send trade offer to winner
						if (itemsToGive.Count > 0) {
							string winnerTradeOfferId, winnerMessage = "Congratulations, you have won on CSGO Win Big! Here are your items.";
							winnerTradeOffer.SendWithToken (out winnerTradeOfferId, winnerTradeToken, winnerMessage);
							Log.Success ("Offer sent to winner.");
						} else {
							Log.Info ("No items to give... strange");
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
		public string winnerTradeToken;
		public List<CSGOItemFromWeb> tradeItems;
		public List<CSGOItemFromWeb> profitItems;
	}

	public class CSGOItemFromWeb {
		public long classId;
		public long instanceId;
	}

	//Classes for json bot inventory
	public class BotInventory {
		public bool success;
		public IDictionary<string, inventoryItem> rgInventory;
	}

	public class inventoryItem {
		public long id;
		public long classid;
		public long instanceid;
	}
}
