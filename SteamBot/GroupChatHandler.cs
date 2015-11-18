using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeWebAPI;
using System.Net;
using System.Xml;
//using System.ServiceModel;
//using System.ServiceModel.Web;
//using System.ServiceModel.Syndication;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Timers;
using SteamKit2.Internal;
using Newtonsoft.Json;
using Google.GData.Client;
using Google.GData.Spreadsheets;


namespace SteamBot
{
	
	public class GroupChatHandler : UserHandler
	{
		//JSON files store data, check if this works later. 
		double interval = 10000;
		//TODO ADD CHECK IF THE FILE EXISTS OR NOT
		public static Dictionary<string,string> groupchatsettings = JsonConvert.DeserializeObject<Dictionary<string,string>>(System.IO.File.ReadAllText(@"GroupChatHandler_Settings.json"));
		public string vdcCommand = groupchatsettings["vdcCommand"];
		public string tf2wCommand = groupchatsettings["tf2wCommand"];
		public string impCommand = groupchatsettings["mapListCommand"];
		public string mapListCommand = groupchatsettings["mapListCommand"];
		public string clearcommand = groupchatsettings["clearcommand"];
		public string chatroomID = groupchatsettings["chatroomID"];
		public string tf2maps = groupchatsettings["tf2maps"];
		public string GoogleAPI = groupchatsettings["GoogleAPI"];
		public string CLIENT_ID = groupchatsettings["CLIENT_ID"];
		public string CLIENT_SECRET = groupchatsettings["CLIENT_SECRET"];
		public string SCOPE = groupchatsettings["SCOPE"];
		public string REDIRECT_URI = groupchatsettings["REDIRECT_URI"];

		//TODO add file decleration here for maps.json and stuff, so we can later implement per-chat file storage.
		//TODO add a lot more values once it's confirmed the system is functional


		// Testing SteamID Groupchat = 103582791439544925;
		SteamID Groupchat = 103582791429594873;

		///<summary>Prints the Input to the interface</summary>
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
			return false;
		}

		public SteamKit2.SteamFriends SteamFriends;
		public void OnChatEnter(SteamKit2.SteamFriends.ChatEnterCallback callback){
			Log.Interface ("Entered Chat");
		}
			
		public override void OnLoginCompleted()
		{
			Bot.SteamFriends.JoinChat (new SteamID (Groupchat));
		}
		public override void OnBotCommand(string command)
		{
			if (command.StartsWith ("Say" , StringComparison.OrdinalIgnoreCase)) 
			{
				string send = command.Remove (0, 3);
				Bot.SteamFriends.SendChatRoomMessage (Groupchat, EChatEntryType.ChatMsg, send); //Posts to the chat the entry put in by the bot
			}
			if (command.StartsWith ("Google" , StringComparison.OrdinalIgnoreCase)) 
			{
				register ();
			}
			if (command.StartsWith("!REAUTH" , StringComparison.OrdinalIgnoreCase)) 
				{
				OAuth2Parameters parameters = new OAuth2Parameters ();

				parameters.ClientId = CLIENT_ID;

				parameters.ClientSecret = CLIENT_SECRET;

				parameters.RedirectUri = REDIRECT_URI;

				parameters.Scope = SCOPE;
					//AUTHENTICATE
					string authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl (parameters);
					Log.Interface (authorizationUrl);
					Log.Interface ("Please visit the URL above to authorize your OAuth "
						+ "request token.  Once that is complete, type in your access code to "
						+ "continue...");
					parameters.AccessCode = Console.ReadLine ();

					OAuthUtil.GetAccessToken (parameters);
					string accessToken = parameters.AccessToken;
					Log.Interface ("PLACE THE FOLLOWING IN GoogleAPI in the settings file: " + accessToken);
					
				} 
		
		}

		public override void OnFriendRemove () {}

		public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}

		public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}

		public override void OnTradeMessage (string message) {}

		public override void OnTradeReady (bool ready) {}

		public override void OnTradeError (string error) {}

		public override void OnTradeTimeout () {}

		public override void OnTradeInit() {}

		public override bool OnTradeRequest() {return false;}

		public override void OnTradeSuccess(){}

		public override void OnTradeAwaitingEmailConfirmation(long tradeOfferID){}

		public override void OnTradeAccept(){}

		//TODO ALLOW USERS TO SEND COMMANDS VIA MSG
		public override void OnMessage (string message, EChatEntryType type) {
			SteamID ChatMsg = 0;
			string response = Chatcommands (ChatMsg, ChatMsg, message);
		    if (response != null) 
			{
				SendChatMessage (response);
			}
		}
		public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
		{
			string adminresponse = null;
			//	bool rank = admincheck(sender); IF CODE FAILS USE THIS
			if (admincheck(sender)){
				adminresponse = admincommands (chatID, sender, message);
			}
			string response = Chatcommands(chatID, sender, message);
			if (response != null) {
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, response);
			}
			if (adminresponse != null) {
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, adminresponse);
			}
		}

		/// <summary>
		/// The commands that users can use by msg'ing the system. Returns a string with the appropriate responses
		/// </summary>
		/// <param name="chatID">ChatID of the chatroom</param>
		/// <param name="sender">STEAMID of the sender</param>
		/// <param name="message">The message sent</param>
		public string admincommands (SteamID chatID, SteamID sender, string message)
		{
			if (message.StartsWith (clearcommand , StringComparison.OrdinalIgnoreCase)) 
			{
				return "This feature has been removed, please use !Wipe";
			}
			if (message.StartsWith ("!wipe", StringComparison.OrdinalIgnoreCase)) 
			{
				string path = @"maps.json";
				File.Delete(path);
				File.WriteAllText (path, "{}");
				return "Wiped Maps";
			}

			return null;
		}
		/// <summary>
		/// The commands that users can use by msg'ing the system. Returns a string with the appropriate responses
		/// </summary>
		/// <param name="chatID">ChatID of the chatroom</param>
		/// <param name="sender">STEAMID of the sender</param>
		/// <param name="message">The message sent</param>
		public string Chatcommands (SteamID chatID, SteamID sender, string message)
		{
			base.OnChatRoomMessage (chatID, sender, message);
			bool rank = admincheck(sender);
			Log.Interface(Bot.SteamFriends.GetFriendPersonaName(sender) + ":" + "(" + rank + ")" + " " + message); 
			Log.Info (Bot.SteamFriends.GetFriendPersonaName (sender) + ": " + message);
			//Retrieves the database of users, checks if they're an admin, and enables admin only commands between brackets
			if (rank) { 
			}

			if (message.StartsWith (vdcCommand , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 5);
				return GoogleSearch(par1, "https://developer.valvesoftware.com/", chatID);
			}
			if (message.StartsWith ("GOOGLE", StringComparison.OrdinalIgnoreCase)) 
			{
				register ();
				return null;
			}

			if (message.StartsWith (tf2maps , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 4);
				return GoogleSearch(par1, "http://tf2maps.net/", chatID);
			}

			if (message.StartsWith ("You have a leak" , StringComparison.OrdinalIgnoreCase)) 
			{
				string output = "https://developer.valvesoftware.com/wiki/leak";
				return output;
				//Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, output);
			}
			if (message.StartsWith (tf2wCommand , StringComparison.OrdinalIgnoreCase)) 
			{
				string par1 = message.Remove (0, 5);
				return GoogleSearch(par1, "https://wiki.teamfortress.com/", chatID);
			}
			if (message.StartsWith ("ghost?" , StringComparison.OrdinalIgnoreCase)) 
			{
				return "no";
			//	Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "no");
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
			if (message.StartsWith("!Remove" ,  StringComparison.OrdinalIgnoreCase))
			{
				string map = message.Remove (0, 8);
				string removed = ImpRemove (map, sender);
				return removed;
				//Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, removed);
			}	
			if (message.StartsWith (impCommand , StringComparison.OrdinalIgnoreCase)) 
			{
				return "This feature has been removed, please use !add <mapname> <downloadurl>";
				//Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "Added:maps");
			}
			if (message.StartsWith ("!add" , StringComparison.OrdinalIgnoreCase)) 
			{
				string[] words = message.Split(' ');
				Log.Interface (words.LongLength.ToString () + " " + words.Length.ToString());
				if (words.Length < 3)
				{
					return "Missing commands! Please use the format: !add Mapname DownloadUrl";
				} 
				else 
				{
				string status = ImpEntry (words [1], words [2], sender);
					//register();
					return status;
				}
			}
			if (message.StartsWith ("!Sheet", StringComparison.OrdinalIgnoreCase)) {
				return "https://docs.google.com/spreadsheets/d/1BGqQLnUFc2tO8NhALm7eLGlFRTSteMTGb5v4isVjK6o/edit#gid=185531839";
			}
			if (message.StartsWith ("!view" , StringComparison.OrdinalIgnoreCase)) 
			{
				Dictionary<string,Tuple<string,SteamID> > entrydata = JsonConvert.DeserializeObject<Dictionary<string,Tuple<string,SteamID>>>(System.IO.File.ReadAllText(@"maps.json"));
				string Maplisting = "";
				string DownloadListing = "";
				foreach (var item in entrydata) 
				{
					Maplisting = Maplisting + item.Key + " , ";
					DownloadListing = DownloadListing + item.Value.Item1 + " , ";
				}
				return Maplisting ;
				//Bot.SteamFriends.SendChatMessage (sender, EChatEntryType.ChatMsg, DownloadListing);
				//Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, Maplisting);
			}
			if (message.StartsWith (mapListCommand, StringComparison.OrdinalIgnoreCase)) 
			{
				string path = @"logs\maps.log";
				// Open the file to read from.
				string readText = File.ReadAllText(path);
				Log.Interface (readText);
				//Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, readText);
				return readText;
			}
			return null;
		}
		/// <summary>
		/// Adds a map to the database
		/// </summary>
		public string ImpEntry(string map , string downloadurl , SteamID sender)
		{
			//Deserialises the current map list
			string response = "Failed to add map to the list";
			Dictionary<string,Tuple<string,SteamID> > entrydata = JsonConvert.DeserializeObject<Dictionary<string,Tuple<string,SteamID>>>(System.IO.File.ReadAllText(@"maps.json"));
			if (entrydata.ContainsKey (map)) { //if it already exists, it deletes it so it can update the data
				Log.Interface("duplicate");
				response = "Error, the entry already exists! Please remove the existing entry";
			} else {
				//Adds the entry
				entrydata.Add (map, new Tuple<String, SteamID> (downloadurl, sender));
				//Saves the data
				System.IO.File.WriteAllText (@"maps.json", JsonConvert.SerializeObject (entrydata));
				response = "Added " + map; 
			}
			return response;
		}
		/// <summary>
		/// Removes specified map from the database.
		/// Checks if the user is an admin or the setter
		/// </summary>
		public string ImpRemove (string map , SteamID sender)
		{
			Log.Interface ("Map: " + map + " Sender: " + sender.ToString ());
			Dictionary<string,Tuple<string,SteamID> > Maplist = JsonConvert.DeserializeObject<Dictionary<string,Tuple<string,SteamID>>> (System.IO.File.ReadAllText (@"maps.json"));
			Dictionary<string,Tuple<string,SteamID> > NewMaplist = new Dictionary<string, Tuple<string, SteamID>>();
			string removed = "none"; 
			foreach (var item in Maplist) {
				//TODO DEBUG
				if (item.Key == map && (admincheck (sender) || sender == item.Value.Item2)) {
					removed = map;
				} else {
					NewMaplist.Add (item.Key, item.Value);
				}
			}
			System.IO.File.WriteAllText(@"maps.json", JsonConvert.SerializeObject(NewMaplist));
			Log.Interface ("Removed: " + removed);
			return removed;
			{
			}
		}
		///<summary> Checks if the given STEAMID is an admin in the database</summary>
		public bool admincheck(SteamID sender)
		{
			//string filedata = System.IO.File.ReadAllText(@"users.json");
			Dictionary<string,EClanPermission> Dictionary = JsonConvert.DeserializeObject<Dictionary<string,EClanPermission>>(System.IO.File.ReadAllText(@"users.json"));
			if (Dictionary.ContainsKey (sender.ToString ())) { //If the STEAMID is in the dictionary
				string Key = sender.ToString (); 
				EClanPermission UserPermissions = Dictionary [Key]; //It will get the permissions value
				if((UserPermissions & EClanPermission.OwnerOfficerModerator) > 0) //Checks if it has sufficient privilages
				{
					return true; //If there's sufficient privilages, it'll return true
				}
			}
			return false; //If there is no entry in the database, or there aren't sufficient privalages, it'll return false
		}

		/// <summary>
		/// Makes a google search, and restricts results to only a single URL.
		/// Returns the URL of the first result
		/// </summary>
		// TODO Clean up the code so it properly uses the google API and is a return function
		public string GoogleSearch(string searchquerey , string url, SteamID chatID) {
			WebClient client = new WebClient ();
			string search = "http://www.google.com.au/search?q=" + searchquerey + "+site:" + url;
			string httpdata = client.DownloadString (search);
			string[] suffix = httpdata.Split (new string[] { "<h3 class=\"r\"><a href=\"/url?q=" + url }, System.StringSplitOptions.None);
			Log.Info (httpdata);
			if (suffix.LongLength > 1) {
				string suffix_string = suffix [1];
				string[] suffix_split = suffix_string.Split (new string[] { "&" }, System.StringSplitOptions.None);
				string page = suffix_split [0];
				string output = url + page;
				Log.Info ("requested:" + output);
				return output;
			} 
			else 
			{
				return "Invalid Search";
			}
		}
			
		//TODO Phase this system out slowly and replace with the existing
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
		//TODO google spreadsheet functions



		public void register ()
		{
			//TODO LESS HARDCODED, JUST RUN AND PLAY POSSIBILITIES

			OAuth2Parameters parameters = new OAuth2Parameters ();

			parameters.ClientId = CLIENT_ID;

			parameters.ClientSecret = CLIENT_SECRET;

			parameters.RedirectUri = REDIRECT_URI;

			parameters.Scope = SCOPE;

			if (GoogleAPI == null) {
				//AUTHENTICATE
				string authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl (parameters);
				Log.Interface (authorizationUrl);
				Log.Interface ("Please visit the URL above to authorize your OAuth "
				+ "request token.  Once that is complete, type in your access code to "
				+ "continue...");
				parameters.AccessCode = Console.ReadLine ();
		  
				OAuthUtil.GetAccessToken (parameters);
				string accessToken = parameters.AccessToken;
				Log.Interface ("PLACE THE FOLLOWING IN GoogleAPI in the settings file: " + accessToken);
			} 

			else 
				
			{
				parameters.AccessToken = GoogleAPI;
			}

			GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory (null, "MySpreadsheetIntegration-v1", parameters);
			SpreadsheetsService service = new SpreadsheetsService ("MySpreadsheetIntegration-v1");
			service.RequestFactory = requestFactory;

			// Instantiate a SpreadsheetQuery object to retrieve spreadsheets.
			SpreadsheetQuery query = new SpreadsheetQuery ("https://spreadsheets.google.com/feeds/spreadsheets/private/full/1BGqQLnUFc2tO8NhALm7eLGlFRTSteMTGb5v4isVjK6o");

	    	SpreadsheetFeed feed = service.Query (query);

			SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];


			//NEW CODE

			// Get the first worksheet of the first spreadsheet.
			// TODO: Choose a worksheet more intelligently based on your
			// app's needs.
			WorksheetFeed wsFeed = spreadsheet.Worksheets;
			WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries[0];

		   

			// Fetch the cell feed of the worksheet.
			CellQuery cellQuery = new CellQuery(worksheet.CellFeedLink);
			cellQuery.ReturnEmpty = ReturnEmptyCells.yes;
			CellFeed cellFeed = service.Query(cellQuery);

		
			foreach (CellEntry cell in cellFeed.Entries) 
			{
				cell.InputValue = " ";
				cell.Update ();
			}

			worksheet.Cols = 3;
			worksheet.Rows = 25;

			worksheet.Update ();

			Log.Interface (cellFeed.Entries.Count.ToString ());
			// Iterate through each cell, updating its value if necessary.

			Dictionary<string,Tuple<string,SteamID> > entrydata = JsonConvert.DeserializeObject<Dictionary<string,Tuple<string,SteamID>>>(System.IO.File.ReadAllText(@"maps.json"));

			int Entries = 1;

			foreach (var item in entrydata) 
			{
				Entries = Entries + 1; 
				Log.Interface ("A" + Entries.ToString());
				foreach (CellEntry cell in cellFeed.Entries)
				{
					if (cell.Title.Text == "A" + Entries.ToString ()) {
						cell.InputValue = item.Key;
						cell.Update();
					}
					if (cell.Title.Text == "B" + Entries.ToString ()) {
						cell.InputValue = item.Value.Item1;
						cell.Update();
					}
					if (cell.Title.Text == "C" + Entries.ToString ()) {
						cell.InputValue = item.Value.Item2.ToString();
						cell.Update();
					}
			}

		}
		}








		//TODO update all code below to no longer be obsolete


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

		//When the timer is 'ticked' it begins polling
		private void rsspoll_Tick(object sender, EventArgs e)
		{
			LogRSS ("Ran");
			rssfeedupdates();
		}

		//The list of feeds that will be updated, where each will be saved, and if they will be posted to chat, obsolete
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
	}
}

