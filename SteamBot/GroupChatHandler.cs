
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
using SimpleFeedReader;
using Google.Apis.Customsearch;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;



namespace SteamBot
{
    public class GroupChatHandler : UserHandler
    {
        //JSON files store data, check if this works later. 

      

        public class ExtraSettings
        {
            public Tuple<string, string, string, Int32>[] Servers { get; set; }
            public string[] Feeds { get; set; }
            public Dictionary<string, string> Searches { get; set; }
            public Dictionary<string, string> Settings { get; set; }
            public Dictionary<string, string> InstantReplies { get; set; }
        }
        public static UserDatabaseHandler UserDatabase { get; set; }
        public string CLIENT_ID = groupchatsettings["CLIENT_ID"];
        public string CLIENT_SECRET = groupchatsettings["CLIENT_SECRET"];
        public string SCOPE = groupchatsettings["SCOPE"];
        public string REDIRECT_URI = groupchatsettings["REDIRECT_URI"];
        public string GoogleAPI = groupchatsettings["GoogleAPI"];
        public string IntegrationName = groupchatsettings["IntegrationName"];

        public static ExtraSettings ExtraSettingsData = JsonConvert.DeserializeObject<ExtraSettings>(System.IO.File.ReadAllText(@"ExtraSettings.json"));
        public static Dictionary<string, string> DataLOG = ExtraSettingsData.Searches;
        public static Dictionary<string, string> InstantReplies = ExtraSettingsData.InstantReplies;
        public static Tuple<string, string, string, Int32>[] Servers = ExtraSettingsData.Servers;
        public static string[] PreviousData = new string[Servers.Length];
        public static string[] Feeds = ExtraSettingsData.Feeds;
        public static string[] StoredFeeditems = new string[Feeds.Length];

        public static Dictionary<string, string> groupchatsettings = ExtraSettingsData.Settings;
        
        
        public string impCommand = groupchatsettings["impCommand"];
        public string mapListCommand = groupchatsettings["mapListCommand"];
        public string deletecommand = groupchatsettings["deletecommand"];
        public string clearcommand = groupchatsettings["clearcommand"];
        public static string chatroomID = groupchatsettings["chatroomID"];

        public static string OnlineSearch = groupchatsettings["OnlineSearch"];
        public static string CX = groupchatsettings["CX"];
        public static string APIKEY = groupchatsettings["APIKEY"];
       
      
        public static string OnlineSync = groupchatsettings["OnlineSync"];

        public static string MapStoragePath = groupchatsettings["MapStoragePath"];



        public string UploadCheckCommand = "!uploadcheck";
        public static string ServerListUrl = groupchatsettings["MapListUrl"];
        public static bool EnableRSS = true;
   

        public static string PreviousMap1 = " ";
        public static string PreviousMap2 = " ";
        public static string DebugPreviousMap1 = " ";
        public static bool SpreadsheetSync = true;
        
        public static bool DoOnce = true;

        public static string SteamIDCommand = "!SteamID";
        
       
        public VBotCommands VBotCommands { get; private set; }
       

        public static SteamID Groupchat = ulong.Parse(chatroomID);

        public override void OnTradeClose()
        {
            base.OnTradeClose();
        }

        
        

        public static Dictionary<string, Tuple<string, SteamID, string, bool>> Maplist = Maplistfile(MapStoragePath);

        public static Dictionary<string, Tuple<string, SteamID, string, bool>> Maplistfile(string MapStoragePath)
        {
            if (File.Exists(MapStoragePath))

            {
                return JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, SteamID, string, bool>>>(System.IO.File.ReadAllText(@MapStoragePath));
            } else {
                Dictionary<string, Tuple<string, SteamID, string, bool>> EmptyMaplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();
                System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(EmptyMaplist));
                return EmptyMaplist;
            }
        }
       
       
       
        /// <summary>
        /// The bot's actions upon entering chat
        /// </summary>
        /// <param name="callback"></param>
		public void OnChatEnter(SteamKit2.SteamFriends.ChatEnterCallback callback) {
            Log.Interface("Entered Chat");
        }

        /// <summary>
        /// The bot's actions upon logging in
        /// </summary>
        /// <param name="callback"></param>
        public override void OnLoginCompleted()
        {
            Log.Interface("Use 'exec 0 join' to join a chatroom");
            Log.Interface("RSS Enabled By default");
            if (DoOnce == true)
            {

                BackgroundWork.InitTimer();
                BackgroundWork.RSSTimer();
                BackgroundWork.InitMOTDTimer();
                DoOnce = false;
                Log.Interface("Initialised Timers");
            }
        }

        /// <summary>
        /// Commands that the bot can execute, inputted via the console
        /// </summary>
        /// <param name="command"></param>
		public override void OnBotCommand(string command)
        {
            if (command.StartsWith("Say", StringComparison.OrdinalIgnoreCase))
            {
                string send = command.Remove(0, 3);
                Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, send); //Posts to the chat the entry put in by the bot
            }
            if (command.StartsWith("Join", StringComparison.OrdinalIgnoreCase)) {
                Bot.SteamFriends.LeaveChat(new SteamID(Groupchat));
                Bot.SteamFriends.JoinChat(new SteamID(Groupchat));
            }
            if (command.StartsWith("sync", StringComparison.OrdinalIgnoreCase))
            {
                BackgroundWork.SheetSync(true);
            }
            if (command.StartsWith("!AUTHENTICATE", StringComparison.OrdinalIgnoreCase))
            {
                OAuth2Parameters parameters = new OAuth2Parameters();

                parameters.ClientId = CLIENT_ID;

                parameters.ClientSecret = CLIENT_SECRET;

                parameters.RedirectUri = REDIRECT_URI;

                parameters.Scope = SCOPE;

                string authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);
                Log.Interface(authorizationUrl);
                Log.Info(authorizationUrl);
                Log.Debug(authorizationUrl);
                Log.Interface("Please visit the URL above to authorize your OAuth "
                    + "request token.  Once that is complete, type in your access code to "
                    + "continue...");
                parameters.AccessCode = Console.ReadLine();
                OAuthUtil.GetAccessToken(parameters);
                string refreshToken = parameters.RefreshToken;
                Log.Interface("Your refresh token needs to be added to your settings file it is as follows:");
                Log.Interface(refreshToken);
                GoogleAPI = refreshToken;
                Log.Interface("SYNC COMMANDS WILL NOT WORK UNTIL BOT RESTART, PLEASE RESTART");
                groupchatsettings.Remove("GoogleAPI");
                groupchatsettings.Add("GoogleAPI", refreshToken);
            }
        }

        /// <summary>
        /// The Method executed upon the bot being messaged via chat
        /// </summary>
        /// <param name="message"> The message itself</param>
        /// <param name="type"></param>
        public override void OnMessage(string message, EChatEntryType type) {
            SteamID ChatMsg = OtherSID;
            string adminresponse = null;
            string response = VBotCommands.Chatcommands(ChatMsg, ChatMsg, message.ToLower());

            if (response != null)
            {
                SendChatMessage(response);
            }

            if (UserDatabaseHandler.admincheck(OtherSID)) {
               
            //    adminresponse = SteamBot.VBot.VBotCommands.admincommands(OtherSID, message);
            }
            if (adminresponse != null) {
                SendChatMessage(adminresponse);
            }
        }
        /// <summary>
        /// The method executed upon the bot receiving messages in group chat
        /// </summary>
        /// <param name="chatID">The SteamID of the chatroom</param>
        /// <param name="sender"> The SteamID of the person who sent the msg</param>
        /// <param name="message">The message</param>
		public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            BackgroundWork.GhostCheck = 120;
            string adminresponse = null;
            if (UserDatabaseHandler.admincheck(sender)) {
                adminresponse = VBotCommands.admincommands(sender, message);
            }
            string response = null;
            response = VBotCommands.Chatcommands(chatID, sender, message.ToLower());
            if (response != null) {
                if (!UserDatabaseHandler.BanList.ContainsKey(sender.ToString()) || UserDatabaseHandler.admincheck(sender))
                {
                    Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, response);
                }
                else
                {
                    Bot.SteamFriends.SendChatMessage(sender, EChatEntryType.ChatMsg, "You are currently banned from using the bot, hours remaining: " + UserDatabaseHandler.BanList[sender.ToString()]);
                }

                }
            if (adminresponse != null) {
                Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, adminresponse);
            }
        }

        
        
        public static string GetSteamIDFromUrl (string url , bool HumanReadable)
        {
           WebClient client = new WebClient();
           string SteamPage = client.DownloadString(url);

            
                string[] SteamIDPreCut = SteamPage.Split(new string[] { "steamid\":\"" }, StringSplitOptions.None);
            
            string[] SteamIDReturn = SteamIDPreCut[1].Split(new string[] { "\"" }, StringSplitOptions.None);
           

            var Steam64Int = long.Parse(SteamIDReturn[0]);

            var steamId64 = Steam64Int;

            var universe = (steamId64 >> 56) & 0xFF;
            if (universe == 1) universe = 0;

            var accountIdLowBit = steamId64 & 1;

            var accountIdHighBits = (steamId64 >> 1) & 0x7FFFFFF;

            
            // should hopefully produce "STEAM_0:0:35928448"
            if (HumanReadable == true)
            {
                var legacySteamId = "STEAM_" + universe + ":" + accountIdLowBit + ":" + accountIdHighBits;
                return legacySteamId;
            }
            else
            {
                var legacySteamId =  universe + accountIdLowBit + accountIdHighBits;
                return legacySteamId.ToString();
            }
            
        }

        /// <summary>
        /// Checks if the Map is uploaded to a server
        /// </summary>
        /// <returns><c>true</c>, If the map was uploaded, <c>false</c> False if it isn't.</returns>
        /// <param name="Mapname">Mapname</param>
        public static bool UploadCheck(string Mapname){
		if (Mapname.Contains("."))
			{
				Mapname = (Mapname.Split (new string[] { "." }, System.StringSplitOptions.None)).First();
			}
		WebClient client = new WebClient ();
		string httpdata = client.DownloadString (ServerListUrl);
		if (httpdata != null & httpdata.Contains(Mapname))
			{
			return true; 
			}
		return false; 
		}
		
      
        /// <summary>
        /// Makes a google search, and restricts results to only a single URL.
        /// Returns the URL of the first result
        /// </summary>
        public static string AdvancedGoogleSearch(string searchquery, string url, SteamID chatid)
        {
            if (OnlineSearch.StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                WebClient client = new WebClient();
                var search = client.DownloadString("https://www.googleapis.com/customsearch/v1?q=" + searchquery + "&cx=" + CX + "&siteSearch=" + url + "&key=" + APIKEY);
                var obj = JObject.Parse(search);
                var info = (string)obj["items"][0]["link"];
                return info;
            }
            return null;
        }
		
        public GroupChatHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnFriendRemove() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) { }

        public override void OnTradeError(string error) { }

        public override void OnTradeTimeout() { }

        public override void OnTradeInit() { }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeSuccess() { }

        public override void OnTradeAwaitingEmailConfirmation(long tradeOfferID) { }

        public override void OnTradeAccept() { }

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd()
        {
            return false;
        }

        public SteamKit2.SteamFriends SteamFriends;

    }
}

