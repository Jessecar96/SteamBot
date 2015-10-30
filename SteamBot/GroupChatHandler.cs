using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeWebAPI;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Xml;
using System.ServiceModel.Syndication;
using System;
using System.IO;
using System.Text;
namespace SteamBot
{
	public class GroupChatHandler : UserHandler
	{

		public GroupChatHandler (Bot bot, SteamID sid) : base(bot, sid) {}

		public override bool OnGroupAdd()
		{
			return false;
		}

		public override bool OnFriendAdd () 
		{
			return true;
		}

		public override void OnLoginCompleted(){}

		public override void OnFriendRemove () {}

		public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}

		public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}

		public override void OnTradeMessage (string message) {}

		public override void OnTradeReady (bool ready) {}

		public override void OnTradeError (string error) {}

		public override void OnTradeTimeout () {}

		public override void OnTradeInit() {}

		public override bool OnTradeRequest() 
		{
			return false;
		}

		public override void OnTradeSuccess(){}

		public override void OnTradeAwaitingEmailConfirmation(long tradeOfferID){}

		public override void OnTradeAccept(){}

		public override void OnMessage (string message, EChatEntryType type) {
			SendChatMessage(Bot.ChatResponse);
			if (message.StartsWith ("!TF2MAPS" , StringComparison.OrdinalIgnoreCase)) 
			{
				Bot.SteamFriends.JoinChat (new SteamID (103582791429594873));
			}
		}

		public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
		{
			Log.Info (Bot.SteamFriends.GetFriendPersonaName (sender) + ": " + message);
			base.OnChatRoomMessage (chatID, sender, message);
			if (message.StartsWith ("!VDC" , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 5);
				GoogleSearch(par1, "https://developer.valvesoftware.com/" , chatID);
			}

			if (message.StartsWith ("You have a leak" , StringComparison.OrdinalIgnoreCase)) 
			{
				string output = "https://developer.valvesoftware.com/wiki/leak";
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, output);
			}
			if (message.StartsWith ("!TF2" , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 5);
				GoogleSearch(par1, "https://wiki.teamfortress.com/" , chatID);
			}
			if (message.StartsWith ("!DEBUG_01")) 
			{
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "RETURN TO SENDER");
			}

			if (message.StartsWith ("!RSS" , StringComparison.OrdinalIgnoreCase)) 
			{
				string url = "http://fooblog.com/feed";
				XmlReader reader = XmlReader.Create(url);
				SyndicationFeed feed = SyndicationFeed.Load(reader);
				reader.Close();
				foreach (SyndicationItem item in feed.Items)
				{
					string subject = item.Title.Text;    
					string summary = item.Summary.Text;

					Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, subject);
				}
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "RETURN TO SENDER");
			}
			if (message.StartsWith ("!DEBUG_02" , StringComparison.OrdinalIgnoreCase)) 
			{
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, "SEND TO:" + "103582791429594873");
			}
			if (message.StartsWith ("!DEBUG_03" , StringComparison.OrdinalIgnoreCase)) 
			{
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, "GROUP CHAT HANDLED SUCCESSFULLY");
			}

			if (message.StartsWith ("!IMP" , StringComparison.OrdinalIgnoreCase)) 
			{
				string[] words = message.Split(' ');
				string save = message.Substring (5, message.Length - 5);
				maps (save);
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "Added:maps");
			}
			if (message.StartsWith ("!MAPS", StringComparison.OrdinalIgnoreCase)) 
			{
				string path = @"logs\maps.log";
				// Open the file to read from.
				string readText = File.ReadAllText(path);
				Log.Interface (readText);
				SendChatMessage(readText);
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "Sent map list as private message");
			}

			if (message.StartsWith ("!CLEAR" , StringComparison.OrdinalIgnoreCase)) 
			{
				string path = @"logs\maps.log";
				File.Delete(path);
				File.WriteAllText (path, Environment.NewLine);
			}

		}
		public void GoogleSearch(string par1 , string url, SteamID chatID) {

			WebClient client = new WebClient ();
			string search = "http://www.google.com.au/search?q=" + par1 + "+site:" + url;
			string httpdata = client.DownloadString (search);
			string[] suffix = httpdata.Split (new string[] { "<h3 class=\"r\"><a href=\"/url?q=" + url }, System.StringSplitOptions.None);
			Log.Info (httpdata);
			string suffix_string = suffix [1];
			string[] suffix_split = suffix_string.Split (new string[] { "&" }, System.StringSplitOptions.None);
			string page = suffix_split [0];
			string output = url + page;
			Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, output);
			Log.Info ("requested:" + output);
		}
		public static void maps(string map)
		{
			string path = @"logs\maps.log";

			// This text is added only once to the file.
			if (!File.Exists (path)) {
				// Create a file to write to.
				string createText = map;
				File.WriteAllText (path, createText + Environment.NewLine);
			} 
			else 
			{
				// This text is always added, making the file longer over time
				// if it is not deleted.
				string appendText = map + Environment.NewLine;
				File.AppendAllText (path, appendText);

				// Open the file to read from.
				string readText = File.ReadAllText (path);
				Console.WriteLine (readText);
			}
		}
	}
}

