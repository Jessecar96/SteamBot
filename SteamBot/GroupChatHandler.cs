using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeWebAPI;
using System.Net;
//using System.ServiceModel;
//using System.ServiceModel.Web;
using System.Xml;
//using System.ServiceModel.Syndication;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Timers;
using SteamKit2.Internal;
using Newtonsoft.Json;

namespace SteamBot
{
	
	public class GroupChatHandler : UserHandler
	{
		
		public string vdcCommand = "!VDC";
		public string tf2wCommand = "!TF2";
		public string impCommand = "!IMP";
		public string mapListCommand = "!MAPS";
		public string clearcommand = "!COMMAND";
		public string chatroomID = "103582791429594873";
		public string tf2maps = "!tfm";
		double interval = 10000;
		// Testing SteamID Groupchat = 103582791439544925;
		SteamID Groupchat = 103582791429594873;

		private Timer rsspoll;

		//initialises the timer for the RSS poll update
		public void InitTimer()
		{
			rsspoll = new Timer();
			rsspoll.Elapsed += new ElapsedEventHandler(rsspoll_Tick);
			rsspoll.Interval = interval; // in miliseconds
			rsspoll.Start();
			LogRSS ("initialised");
		}

		public static void RUN (string file)
		{

		}
		//When the timer is 'ticked' it begins polling
		private void rsspoll_Tick(object sender, EventArgs e)
		{
			LogRSS ("Ran");
			rssfeedupdates();
		}

		//The list of feeds that will be updated, where each will be saved, and if they will be posted to chat
		public void rssfeedupdates()
		{
			rssloop("https://www.reddit.com/r/AskReddit/new/.rss" , "Debug.log" , false);
			rssloop("http://steamcommunity.com/groups/TF2Mappers/rss/" , "GroupEvents.log" , true);
			rssloop("http://steamcommunity.com/groups/tf2maps_twitter/rss/" , "twitterbot.log" , true);
			rssloop("http://www.teamfortress.com/rss.xml", "tf2site.log", true);
		}

		//Gets the RSS feed, then saves it
		public void rssloop (string feed , string filename , Boolean sendtochat)
		{
			getrssfirstentry (feed);
			string latest = getrssfirstentry(feed);
			RSSWrite (@"logs\", filename, latest, sendtochat);
		}

		//Gets the feed sent, and returns the first entry
		string getrssfirstentry(string feedtoread)
		{
		//	string url = feedtoread ;
		//	XmlReader reader = XmlReader.Create(url);
		//	SyndicationFeed feed = SyndicationFeed.Load(reader);
		//	string file = reader.ToString();
			// Closing this, will it change anything? reader.Close();
		//	var latest = feed.Items.FirstOrDefault().Title.Text;
			return "THIS HAS BEEN FROZEN DUE TO AN ERROR";
		}

		//Will compare the LOG file with the RSS feed's first entry.
		public void RSSWrite (string path , string filename , string content, Boolean SendtoChat)
		{
			string filepath = path + filename;
			if (!File.Exists (filepath)) //Checks if the RSS feed is stored already, if not it writes it
			{
				File.WriteAllText (filepath, content);
			}
			else
			{
				string readText = File.ReadAllText (filepath);
				if (content != readText)
				{ //Checks if the existing entry is the same or different
					File.WriteAllText (filepath, content);
					LogRSS ("New Entry");
					LogRSS (content);
					if (SendtoChat == true)
					{
						Bot.SteamFriends.SendChatRoomMessage (Groupchat, EChatEntryType.ChatMsg, content); //Posts to the chat the new entry if it's set to
					}
				}
				else
				{
					LogRSS ("No new entry"); //Logs that there is no new entry
				}
			}
		}

		//Prints the Input to the screen
		public void LogRSS(string par1)
		{
			Log.Interface (par1);
		}

		public GroupChatHandler (Bot bot, SteamID sid) : base(bot, sid) {}

		public override bool OnGroupAdd()
		{
			return false;
		}

		public override bool OnFriendAdd () 
		{
			return true;
		}

		public SteamKit2.SteamFriends SteamFriends;
		public void OnChatEnter(SteamKit2.SteamFriends.ChatEnterCallback callback){

		}

	
		public override void OnLoginCompleted()
		{
			Bot.SteamFriends.JoinChat (new SteamID (Groupchat));
		}

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
			if (message.StartsWith ("!JOIN" , StringComparison.OrdinalIgnoreCase)) 
			{
				
				Bot.SteamFriends.JoinChat (new SteamID (Groupchat));
			}

			if (message.StartsWith ("!RSS" , StringComparison.OrdinalIgnoreCase)) 
			{
				InitTimer ();
			}
		}

		public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
		{

			base.OnChatRoomMessage (chatID, sender, message);
			//Retrieves the database of users	
			bool value = admincheck(sender); //Sees if the sender is an admin or not
			Log.Interface ("Msg RANK: " + value); //Logs on the screen if the sender is an admin or not
			if (value) { //Checks if the user is Admin from before returned value, if so allows them to use admin commands
				Log.Info ("Admin" + Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message); //Logs admin commands used
				if (message.StartsWith ("clearcommand" , StringComparison.OrdinalIgnoreCase)) 
				{
					string path = @"logs\maps.log";
					File.Delete(path);
					File.WriteAllText (path, Environment.NewLine);
				}
			}
			Log.Info (Bot.SteamFriends.GetFriendPersonaName (sender) + ": " + message);
			if (message.StartsWith (vdcCommand , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 5);
				GoogleSearch(par1, "https://developer.valvesoftware.com/", chatID);
			}

			if (message.StartsWith (tf2maps , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 4);
				GoogleSearch(par1, "http://tf2maps.net/", chatID);
			}

			if (message.StartsWith ("You have a leak" , StringComparison.OrdinalIgnoreCase)) 
			{
				string output = "https://developer.valvesoftware.com/wiki/leak";
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, output);
			}
			if (message.StartsWith (tf2wCommand , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 5);
				GoogleSearch(par1, "https://wiki.teamfortress.com/", chatID);
			}
			if (message.StartsWith ("ghost?" , StringComparison.OrdinalIgnoreCase)) 
			{
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "no");
			}

			if (message.StartsWith ("!RSS" , StringComparison.OrdinalIgnoreCase)) 
			{
				//string url = "http://steamcommunity.com/groups/TF2Mappers/rss/";
				//XmlReader reader = XmlReader.Create(url);
				//SyndicationFeed feed = SyndicationFeed.Load(reader);
				//string file = reader.ToString();
				//reader.Close();
				//var latest = feed.Items.OrderByDescending(x=>x.PublishDate).FirstOrDefault().Title.Text;
				//Log.Interface (latest.ToString());
				//SendChatMessage(latest.ToString());
				//Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, latest.ToString());
			}
			if (message.StartsWith ("!DEBUG_02" , StringComparison.OrdinalIgnoreCase)) 
			{
				//This code has been emptied
			}
			if (message.StartsWith ("!DEBUG_03" , StringComparison.OrdinalIgnoreCase)) 
			{
				//manager.Subscribe<SteamGameCoordinator.MessageCallback>(Onrun);
				//manager.RunCallbacks<SteamGameCoordinator.MessageCallback>(Onrun);
				LogRSS("INITIATED");
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, "I WILL ALWAYS SHOUT");
			}
	
			if (message.StartsWith (impCommand , StringComparison.OrdinalIgnoreCase)) 
			{
				string[] words = message.Split(' ');
				string save = message.Substring (5, message.Length - 5);
				maps (save);
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "Added:maps");
			}
			if (message.StartsWith (mapListCommand, StringComparison.OrdinalIgnoreCase)) 
			{
				string path = @"logs\maps.log";
				// Open the file to read from.
				string readText = File.ReadAllText(path);
				Log.Interface (readText);
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, readText);
			}
		}

		//Checks if the given SteamID is an admin
		public bool   admincheck(SteamID sender)
		{
			//string filedata = System.IO.File.ReadAllText(@"users.json");
			Dictionary<string,EClanPermission> Dictionary = JsonConvert.DeserializeObject<Dictionary<string,EClanPermission>>(System.IO.File.ReadAllText(@"users.json"));
			if (Dictionary.ContainsKey (sender.ToString ())) { //If the STEAMID is in the dictionary
				string Key = sender.ToString (); 
				EClanPermission UserPermissions = Dictionary [Key]; //It will get the permissions value
				if((UserPermissions & EClanPermission.OwnerOfficerModerator) > 0)
					//Checks if it has sufficient privilages
				{
					return true; //if it does, it'll say it does
				}
			}
			return false; //If there is no entry in the database, or there aren't sufficient privalages, it returns false
		}

		public void Onrun(SteamGameCoordinator.MessageCallback callback)
		{
			LogRSS ("SKEETSKEET");

		}

		public void GoogleSearch(string par1 , string url, SteamID chatID) {
			
			WebClient client = new WebClient ();
			string search = "http://www.google.com.au/search?q=" + par1 + "+site:" + url;
			string httpdata = client.DownloadString (search);
			string[] suffix = httpdata.Split (new string[] { "<h3 class=\"r\"><a href=\"/url?q=" + url }, System.StringSplitOptions.None);
			Log.Info (httpdata);
			if (suffix.LongLength > 1) {
				string suffix_string = suffix [1];
				string[] suffix_split = suffix_string.Split (new string[] { "&" }, System.StringSplitOptions.None);
				string page = suffix_split [0];
				string output = url + page;
				Log.Info ("requested:" + output);
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, output);
			} 
			else 
			
			{
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "Invalid Search");
			}
		}



		public static void maps(string map)
		{
			string path = @"logs\maps.log";

			// This text is added only once to the file.
			if (!File.Exists (path)) {
				// Create a file to write to.
				string createText = map;
				File.WriteAllText (path, createText + " , ");
			} 
			else 
			{
				// This text is always added, making the file longer over time
				// if it is not deleted.
				string appendText = map + " , ";
				File.AppendAllText (path, appendText);

				// Open the file to read from.
				string readText = File.ReadAllText (path);
				Console.WriteLine (readText);
			}
		}
	}
}

