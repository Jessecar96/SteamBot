
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
        double interval = 5000;
        public static int GhostCheck = 120;
        private Timer Tick;
        private Timer MOTDTick;
        private Timer RSSTick;
        private int MOTDPosted = 0;
        double MOTDHourInterval = 1;
        public static string MOTD = null;
        public static string MOTDSetter = null;

        public class ExtraSettings
        {
            public Tuple<string, string, string, Int32>[] Servers { get; set; }
            public string[] Feeds { get; set; }
            public Dictionary<string, string> Searches { get; set; }
            public Dictionary<string, string> Settings { get; set; }
            public Dictionary<string, string> InstantReplies { get; set; }
        }

        public static ExtraSettings ExtraSettingsData = JsonConvert.DeserializeObject<ExtraSettings>(System.IO.File.ReadAllText(@"ExtraSettings.json"));
        public static Dictionary<string, string> DataLOG = ExtraSettingsData.Searches;
        public static Dictionary<string, string> InstantReplies = ExtraSettingsData.InstantReplies;
        public static Tuple<string, string, string, Int32>[] Servers = ExtraSettingsData.Servers;
        public static string[] PreviousData = new string[Servers.Length];
        public static string[] Feeds = ExtraSettingsData.Feeds;
        public static string[] StoredFeeditems = new string[Feeds.Length];
        public static Dictionary<string, string> groupchatsettings = ExtraSettingsData.Settings;
        public static string UserDatabaseFile = "users.json";
        public static Dictionary<string, EClanPermission> UserDatabase = UserDatabaseRetrieve(UserDatabaseFile);
        public string impCommand = groupchatsettings["impCommand"];
        public string mapListCommand = groupchatsettings["mapListCommand"];
        public string deletecommand = groupchatsettings["deletecommand"];
        public string clearcommand = groupchatsettings["clearcommand"];
        public static string chatroomID = groupchatsettings["chatroomID"];
        public string OnlineSearch = groupchatsettings["OnlineSearch"];
        public string CX = groupchatsettings["CX"];
        public string APIKEY = groupchatsettings["APIKEY"];
        public string GoogleAPI = groupchatsettings["GoogleAPI"];
        public string CLIENT_ID = groupchatsettings["CLIENT_ID"];
        public string CLIENT_SECRET = groupchatsettings["CLIENT_SECRET"];
        public string SCOPE = groupchatsettings["SCOPE"];
        public string REDIRECT_URI = groupchatsettings["REDIRECT_URI"];
        public string OnlineSync = groupchatsettings["OnlineSync"];
        public string SpreadSheetURI = "https://spreadsheets.google.com/feeds/spreadsheets/private/full/" + groupchatsettings["SpreadSheetURI"];
        public string IntegrationName = groupchatsettings["IntegrationName"];
        public static string MapStoragePath = groupchatsettings["MapStoragePath"];
        public string HelpLink = groupchatsettings["HelpLink"];

        public string UploadCheckCommand = "!uploadcheck";
        public string ServerListUrl = groupchatsettings["MapListUrl"];
        public static bool EnableRSS = false;
   

        public static string PreviousMap1 = " ";
        public static string PreviousMap2 = " ";
        public static string DebugPreviousMap1 = " ";
        public static bool SpreadsheetSync = true;
        public static bool SyncRunning = false;
        public static bool DoOnce = true;

        public static string SteamIDCommand = "!SteamID";
        public static string BanListFilePath = "Banlist.json";
        public static string[] Debug = { "debug_01", "debug_02","debug_03"};

        public static string InstantRepliesFile = "instantreplies.json";



        static Dictionary<string, Tuple<string, List<string>>> ChatCommandsArray = JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, List<string>>>>(System.IO.File.ReadAllText(@"ChatCommands.json"));


        static Dictionary<string, Tuple<string, SteamID>> InstantChatReplies = InstantChatRepliesData(InstantRepliesFile);


        static Dictionary<string, Tuple<string, SteamID>> InstantChatRepliesData(string FileDirectory)
        {
            {
                if (File.Exists(FileDirectory))

                {
                    return JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, SteamID>>>(System.IO.File.ReadAllText(@FileDirectory));

                }
                else {
                    Dictionary<string, Tuple<string,SteamID>> EmptyRepliesFile = new Dictionary<string, Tuple<string,SteamID>>();
                    System.IO.File.WriteAllText(@FileDirectory, JsonConvert.SerializeObject(EmptyRepliesFile));
                    return EmptyRepliesFile;
                }
            }
        }

        public static Dictionary<string, int> BanList = BanListFile(BanListFilePath);

        public static Dictionary<string, int> BanListFile(string BanListFilePath)
        {
            if (File.Exists(BanListFilePath))

            {
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(System.IO.File.ReadAllText(@BanListFilePath));
            }
            else {
                Dictionary<string, int> EmptyBanListFile = new Dictionary<string, int>();
                System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(EmptyBanListFile));
                return EmptyBanListFile;
            }
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
        public static Dictionary<string, EClanPermission> UserDatabaseRetrieve(string UserDatabase) { //TODO have this load with the bot

            if (!File.Exists(UserDatabase))
            {
                System.IO.File.WriteAllText(@UserDatabase, JsonConvert.SerializeObject(new Dictionary<string, EClanPermission>()));
                Dictionary<string, EClanPermission> UserDatabaseData = new Dictionary<string, EClanPermission>();
                return UserDatabaseData;
            }
            return JsonConvert.DeserializeObject<Dictionary<string, EClanPermission>>(System.IO.File.ReadAllText(@UserDatabase));
        }
        SteamID Groupchat = ulong.Parse(chatroomID);
        /// <summary>
        /// Initialises the main timer
        /// </summary>
        public void InitTimer()
        {
            Tick = new Timer();
            Tick.Elapsed += new ElapsedEventHandler(TickTasks);
            Tick.Interval = interval; // in miliseconds
            Tick.Start();
        }

        /// <summary>
        /// Initialises the Timer that RSS feeds will be checked on
        /// </summary>
        public void RSSTimer()
        {
            RSSTick = new Timer();
            RSSTick.Elapsed += new ElapsedEventHandler(RSSTracker);
            RSSTick.Interval = 30000; // in miliseconds
            RSSTick.Start();
        }

        /// <summary>
        /// Initialises the MOTD timer
        /// </summary>
        public void InitMOTDTimer()
        {
            MOTDTick = new Timer();
            MOTDTick.Elapsed += new ElapsedEventHandler(MOTDPost);
            MOTDTick.Interval = MOTDHourInterval * 1000 * 60 * 60; // in miliseconds TODO update this to formulate once a day
            MOTDTick.Start();

        }
        /// <summary>
        /// Posts the MOTD to the group chat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MOTDPost(object sender, EventArgs e)
        {
            if ((MOTD == null) | (MOTDPosted >= 24))
            {
                MOTD = null;
                Log.Interface("No MOTD set");
                MOTDPosted = 0;
            }
            else
            {
                MOTDPosted = MOTDPosted + 1;
                Bot.SteamFriends.SetPersonaName("MOTD");
                Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, MOTD);
                Bot.SteamFriends.SetPersonaName("[" + Maplist.Count.ToString() + "] " + Bot.DisplayName);
            }
            foreach (KeyValuePair<string, int> Key in BanList)
            {
                BanList.Remove(Key.Key);
                BanList.Add(Key.Key, Key.Value - 1);
            }
            System.IO.File.WriteAllText(@BanListFilePath, JsonConvert.SerializeObject(BanList));
    }

    /// <summary>
    /// The Main Timer's method, executed per tick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void TickTasks(object sender, EventArgs e)
        {
            if (SpreadsheetSync)
            {
                SpreadsheetSync = false;
                SheetSync(false);
            }
            MapChangeTracker();
            GhostCheck = GhostCheck - 1;
            if (GhostCheck <= 1)
            {
                GhostCheck = 120;
                Bot.SteamFriends.LeaveChat(new SteamID(Groupchat));
                Bot.SteamFriends.JoinChat(new SteamID(Groupchat));
            }
        }

        /// <summary>
        /// Tracks all maps in the server list and posts to the group when there's a map change
        /// </summary>
        public void MapChangeTracker()
        {
            int count = 0;


            foreach (Tuple<string, string, string, Int32> ServerAddress in Servers)
            {
                Steam.Query.ServerInfoResult ServerData = ServerQuery(System.Net.IPAddress.Parse(ServerAddress.Item2), ServerAddress.Item4);
                if ((ServerData.Map != PreviousData[count]) && ServerData.Players > 2)
                {
                    Tuple<string, SteamID> Mapremoval = ImpRemove(ServerData.Map, 0, true , null);
                    Bot.SteamFriends.SendChatMessage(Mapremoval.Item2, EChatEntryType.ChatMsg, "Hi, your map: " + Mapremoval.Item1 + " is being played on the " + ServerAddress.Item1 + "!");
                    Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, "Map changed to: " + ServerData.Map.ToString() + " on the " + ServerAddress.Item1 + " " + ServerData.Players + "/" + ServerData.MaxPlayers + " - Join at: steam://connect/" + ServerAddress.Item2 + ":" + ServerAddress.Item4);
                   
                    SpreadsheetSync = true;
                }
                PreviousData[count] = ServerData.Map;
                count = count + 1;
            }

            
        }

        /// <summary>
        /// Queries the server and returns the information
        /// </summary>
        /// <param name="ipadress">Ipadress that will be queried</param>
        /// <param name="port"> The port that will be used, typically 27015 </param> 
        Steam.Query.ServerInfoResult ServerQuery(System.Net.IPAddress ipaddress, Int32 port)
        {
            IPEndPoint ServerIP = new IPEndPoint(ipaddress, port);
            Steam.Query.Server Information = new Steam.Query.Server(ServerIP);
            Steam.Query.ServerInfoResult ServerInformation = Information.GetServerInfo().Result;
            return ServerInformation;
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
            Log.Interface("RSS disabled By default");
            if (DoOnce == true)
            {
                InitTimer();
                RSSTimer();
                InitMOTDTimer();
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
                SheetSync(true);
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
            string response = Chatcommands(ChatMsg, ChatMsg, message.ToLower());

            if (response != null)
            {
                SendChatMessage(response);
            }

            if (admincheck(OtherSID)) {
                adminresponse = admincommands(OtherSID, message);
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
            GhostCheck = 120;

            string adminresponse = null;

            if (admincheck(sender)) {
                adminresponse = admincommands(sender, message);
            }
            string response = null;
           
        
                if (!BanList.ContainsKey(sender.ToString()) || admincheck(sender))
                {
                    response = Chatcommands(chatID, sender, message.ToLower());
                    if (response != null)
                    {
                        Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, response);
                    }
                }
                else
                {
                   
                }

               
            if (adminresponse != null) {
                Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, adminresponse);
            }
        }

        /// <summary>
        /// The commands that users can use by msg'ing the system. Returns a string with the appropriate responses
        /// </summary>
        /// <param name="chatID">ChatID of the chatroom</param>
        /// <param name="message">The message sent</param>
        public string admincommands(SteamID sender, string FullMessage)
        {

            FullMessage.Replace(@"\s+", " ");
            string[] Words = FullMessage.Split();
            string Message = FullMessage.Remove(0, ((Words[0].Length) * (Words.Length > 1).GetHashCode()) + (Words.Length > 1).GetHashCode());

            if (DoesMessageStartWith(Words[0], ChatCommandsArray["Rejoin"].Item2))
                if (FullMessage.StartsWith("!ReJoin", StringComparison.OrdinalIgnoreCase))
                {
                    Bot.SteamFriends.LeaveChat(new SteamID(Groupchat));
                    Bot.SteamFriends.JoinChat(new SteamID(Groupchat));
                }
            if (Words[0].StartsWith("!Say", StringComparison.OrdinalIgnoreCase))
            {
                Bot.SteamFriends.SendChatRoomMessage(Groupchat, EChatEntryType.ChatMsg, Message);
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["SetMOTD"].Item2))
            {
                if (Message != null)
                {

                    if (MOTD != null)
                    {
                        return "There is currently a MOTD, please remove it first";
                    }
                    else
                    {
                        MOTDSetter = Bot.SteamFriends.GetFriendPersonaName(sender) + " " + sender;
                        MOTD = Message;
                        return "MOTD Set to: " + Message;
                    }
                }
                else
                {
                    return "Make sure to include a MOTD to display!";
                }
            }
            if (Message.StartsWith("Say my name", StringComparison.OrdinalIgnoreCase))
            {
                return Bot.SteamFriends.GetFriendPersonaName(sender);
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["RemoveMOTD"].Item2))
            {
                MOTD = null;
                return "Removed MOTD";
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["ClearCommands"].Item2))
            {
                Maplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();
                System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(Maplist));
                SpreadsheetSync = true;
                return "Wiped all Maps";
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["EnableSync"].Item2))
            {
                OnlineSync = "true";
                groupchatsettings.Remove("OnlineSync");
                groupchatsettings.Add("OnlineSync", "true");
                System.IO.File.WriteAllText(@"ExtraSettings.json", JsonConvert.SerializeObject(ExtraSettingsData));
                return "Enabled Sync";
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["DisableSync"].Item2))
            {
                OnlineSync = "false";
                groupchatsettings.Remove("OnlineSync");
                groupchatsettings.Add("OnlineSync", "false");
                System.IO.File.WriteAllText(@"ExtraSettings.json", JsonConvert.SerializeObject(ExtraSettingsData));
                return "Disabled Sync";
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["EnableRSS"].Item2))
            {
                EnableRSS = true;
                return "Enabled RSS";
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["DisableRSS"].Item2))
            {
                EnableRSS = false;
                return "Disabled RSS";
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["Rejoin"].Item2))
            {
                Bot.SteamFriends.LeaveChat(new SteamID(Groupchat));
                Bot.SteamFriends.JoinChat(new SteamID(Groupchat));
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["Unban"].Item2))
            {
                if (Words.Length > 1) ;
                string Userid = GetSteamIDFromUrl(Words[1], true);
                if (BanList.ContainsKey(Userid.ToString()))
                {
                    BanList.Remove(Userid);
                    return "The ban has now been lifted";
                }
                else
                {
                    return "User is not banned";
                }
            }

            if (DoesMessageStartWith(Words[0], ChatCommandsArray["AddCommands"].Item2))
            {
                if (Words.Length > 1)
                {
                    foreach (KeyValuePair<string, Tuple<string, List<string>>> Entry in ChatCommandsArray)
                    {
                        if (Entry.Value.Item2.Contains(Words[1], StringComparer.OrdinalIgnoreCase))

                        {
                            foreach (KeyValuePair<string, Tuple<string, List<string>>> EntryRecheck in ChatCommandsArray)
                            {
                                if (EntryRecheck.Value.Item2.Contains(Words[2], StringComparer.OrdinalIgnoreCase))

                                {
                                    return "Sorry this command already exists!";
                                }
                                else
                                {
                                    if (Words[2].StartsWith("!"))
                                    {
                                        Entry.Value.Item2.Add(Words[2]);
                                    }
                                    else
                                    {
                                        Entry.Value.Item2.Add("!" + Words[2]);
                                    }
                                    System.IO.File.WriteAllText(@"ChatCommands.json", JsonConvert.SerializeObject(ChatCommandsArray));
                                    return "Added: " + Words[2] + " to " + Entry.Key;
                                }

                            }

                            return "Command not found";
                        }

                    }
                }
                else
                {
                    return HelpLink;
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["RemoveCommands"].Item2))
            {
                if (Words.Length > 1)
                {
                    foreach (KeyValuePair<string, Tuple<string, List<string>>> Entry in ChatCommandsArray)
                    {
                        if (Entry.Value.Item2.Contains(Words[1], StringComparer.OrdinalIgnoreCase))
                            {
                                Entry.Value.Item2.Remove(Words[1]);
                                System.IO.File.WriteAllText(@"ChatCommands.json", JsonConvert.SerializeObject(ChatCommandsArray));
                                return "Removed: " + Words[1];
                            }
                            else
                            {
                                return "Couldn't find command, try again";
                            }

                    }
                }
                else
                {
                    return HelpLink;
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["Ban"].Item2))

            {


                if (Words.Length > 3)
                {
                    string[] Reason = Message.Split(new string[] { " " + Words[2] + " " }, StringSplitOptions.RemoveEmptyEntries);

                    SteamID Userid = new SteamID(GetSteamIDFromUrl(Words[1], true));

                    if (BanList.ContainsKey(Userid.ToString()))
                    {
                        return "This user has already been banned, their ban has " + BanList[Userid.ToString()] + " remainig";
                    }
                    BanList.Add(Userid.ToString(), int.Parse(Words[2]) * 24);
                    System.IO.File.WriteAllText(@BanListFilePath, JsonConvert.SerializeObject(BanList));

                    Bot.SteamFriends.SendChatMessage(Userid, EChatEntryType.ChatMsg, "You have been banned from using all bot features for " + Words[2] + "days. Reason given: " + Reason[1]);
                    return "Banned user:" + Userid.ToString() + " (" + Bot.SteamFriends.GetFriendPersonaName(Userid).ToString() + ") for: " + Words[2] + " days. Reason given: " + Reason[1];
                }
                else
                {
                    return "The command is: " + "!Ban" + " <url of user>, Duration in days, Reason";
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["RemoveReply"].Item2) & Words.Length > 1)
            {
                if (ChatCommandsArray.ContainsKey(Words[1]))
                {
                    return "Sorry that reply already exists";
                }
                foreach (KeyValuePair<string, Tuple<string, SteamID>> Entry in InstantChatReplies)
                {
                    if ((Entry.Key == Words[1]) && (sender == Entry.Value.Item2) | admincheck(sender))
                    {
                        Bot.SteamFriends.SendChatMessage(Entry.Value.Item2, EChatEntryType.ChatMsg, "Hi, your instant reply has been removed.");
                        InstantChatReplies.Remove(Words[1]);
                        System.IO.File.WriteAllText(@InstantRepliesFile, JsonConvert.SerializeObject(InstantChatReplies));

                    }
                }

            }
            return null;
        }



        /// <summary>
        /// The commands that users can use by msg'ing the system. Returns a string with the appropriate responses
        /// </summary>
        /// <param name="chatID">ChatID of the chatroom</param>
        /// <param name="sender">STEAMID of the sender</param>
        /// <param name="message">The message sent</param>
        public string Chatcommands(SteamID chatID, SteamID sender, string FullMessage)
        {
            base.OnChatRoomMessage(chatID, sender, FullMessage);

            bool rank = admincheck(sender);

            FullMessage.Replace(@"\s+", " ");
            string[] Words = FullMessage.Split();

            string Message = FullMessage.Remove(0, ((Words[0].Length) * (Words.Length > 1).GetHashCode()) + (Words.Length > 1).GetHashCode());

            
            foreach (KeyValuePair<string, string> Entry in DataLOG) //TODO Disable autocorrections
            {
                if (Words[0].StartsWith(Entry.Key, StringComparison.OrdinalIgnoreCase))
                {

                    return AdvancedGoogleSearch(Message, Entry.Value, chatID);
                }
            }

            if (DoesMessageStartWith(Words[0], ChatCommandsArray["SteamIDCommand"].Item2))
            {
                if (Words.Length > 0)
                {
                    return GetSteamIDFromUrl(Words[1], true);
                }
                else {
                    return "URL is missing, please add a url";
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["MySteamIDCommand"].Item2))
            {
                return sender.ToString();
            }

            if (DoesMessageStartWith(Words[0], ChatCommandsArray["Rejoin"].Item2))
            {
                Bot.SteamFriends.JoinChat(new SteamID(Groupchat));
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["MOTDSetter"].Item2))
            {
                return MOTDSetter;
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["MOTDTick"].Item2))
            {
                return MOTDPosted.ToString();
            }

            if (DoesMessageStartWith(Words[0], ChatCommandsArray["MOTD"].Item2))
            {
                return MOTD;
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["Sync"].Item2))
            {
                SheetSync(true);
                return null;
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["UploadCheckCommand"].Item2))
            {
                if (Words.Length > 0)
                {
                    return UploadCheck(Words[1]).ToString();
                }
                else {
                    return "No map specified";
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["DeleteCommands"].Item2))
            {
                string[] Reason = Message.Split(new string[] { Words[1] }, StringSplitOptions.None);


                Tuple<string, SteamID> removed = ImpRemove(Words[1], sender, false, Reason[1]);
                return "Removed map: " + removed.Item1;
            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["ImpCommands"].Item2))
            {

                if (Words.Length == 1)
                {
                    return "!add <mapname> <url> <notes> is the command. however if the map is uploaded you do not need to include the url";
                }
                int length = (Words.Length > 2).GetHashCode(); //Checks if there are more than 3 or more words
                int Uploaded = (UploadCheck(Words[1])).GetHashCode(); //Checks if map is uploaded. Crashes if only one word //TODO FIX THAT
                string UploadStatus = "Uploaded"; //Sets a string, that'll remain unless altered
                if (length + Uploaded == 0)
                { //Checks if either test beforehand returned true
                    return "Make sure to include the download URL!";
                }
                else {
                    string[] notes = FullMessage.Split(new string[] { Words[2 - Uploaded] }, StringSplitOptions.None); //Splits by the 2nd word (the uploadurl) but if it's already uploaded, it'll split by the map instead 
                    if (Uploaded == 0) //If the map isn't uploaded, it'll set the upload status to the 3rd word (The URL)
                    {
                        UploadStatus = Words[2];
                    }
                    string status = ImpEntry(Words[1], UploadStatus, notes[1], sender); //If there are no notes, but a map and url, this will crash.
                    SpreadsheetSync = true;
                    return status;
                }


            }
            if (DoesMessageStartWith(Message, ChatCommandsArray["Update"].Item2))
            {
                if (Words.Length <= 2)  //Checks if there are less than 2 words, the previous map and map to change
                {
                    return "!updatemap <PreviousMapName> <NewMapName> <url> <notes> is the command.";
                }

                string[] FullMessageCutArray = Words.Skip(2).ToArray();

                int length = (FullMessageCutArray.Length > 1).GetHashCode(); //Checks if there are more than 3 or more words

                int Uploaded = 0;

                if (Words.Length > 0)
                {
                    Uploaded = (UploadCheck(FullMessageCutArray[0])).GetHashCode();
                }
                //Log.Interface("Checking if" + UploadCheck(FullMessageCutArray[1]) + "Is uploaded");

                //Checks if map is uploaded. Crashes if only one word //TODO FIX THAT
                string UploadStatus = "Uploaded"; //Sets a string, that'll remain unless altered
                if (length + Uploaded == 0)
                { //Checks if either test beforehand returned true
                    return "Make sure to include the download URL!";
                }
                else {
                    string[] notes = FullMessage.Split(new string[] { FullMessageCutArray[1 - Uploaded] }, StringSplitOptions.None); //Splits by the 2nd word (the uploadurl) but if it's already uploaded, it'll split by the map instead 
                    if (Uploaded == 0) //If the map isn't uploaded, it'll set the upload status to the 3rd word (The URL)
                    {
                        UploadStatus = FullMessageCutArray[1];
                    }
                    string status = ImpEntryUpdate(Words[1], FullMessageCutArray[0], UploadStatus, notes[1], sender); //If there are no notes, but a map and url, this will crash.
                    SpreadsheetSync = true;
                    return status;
                }

            }
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["MapCommands"].Item2))
            {
                // Dictionary<string, Tuple<string, SteamID>> entrydata = JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, SteamID>>>(System.IO.File.ReadAllText(@MapStoragePath)); //TODO can we get rid of this?

                string Maplisting = "";
                string DownloadListing = "";
                foreach (var item in Maplist)
                {
                    Maplisting = Maplisting + item.Key + " , ";
                    DownloadListing = DownloadListing + item.Value.Item1 + " , ";
                }
                Bot.SteamFriends.SendChatMessage(sender, EChatEntryType.ChatMsg, DownloadListing);
                return Maplisting;
            }
            foreach (Tuple<string, string, string, Int32> ServerAddress in Servers)
            {
                if (Words[0].StartsWith(ServerAddress.Item3, StringComparison.OrdinalIgnoreCase))
                {
                    Steam.Query.ServerInfoResult ServerData = ServerQuery(System.Net.IPAddress.Parse(ServerAddress.Item2), ServerAddress.Item4);
                    return ServerData.Map + " " + ServerData.Players + "/" + ServerData.MaxPlayers + " - Join at: steam://connect/" + ServerAddress.Item2 + ":" + ServerAddress.Item4;
                }
            }

            if (DoesMessageStartWith(Words[0], ChatCommandsArray["HelpCommands"].Item2))
            {
                if (Words.Length > 1)
                {
                    foreach (KeyValuePair<string, Tuple<string, List<string>>> Entry in ChatCommandsArray)
                    {
                        if (Entry.Value.Item2.Contains(Words[1] , StringComparer.OrdinalIgnoreCase))
                        
                        {
                            return Entry.Key + ": " + Entry.Value.Item1 + " Commands: " + string.Join(", ", Entry.Value.Item2); ;
                        }

                    }
                }
                else
                {
                    return HelpLink;
                }
            }

            if (DoesMessageStartWith(Words[0], ChatCommandsArray["AddReplies"].Item2) & Words.Length > 2)
            {
                if(InstantChatReplies.ContainsKey(Words[1]))
                {
                    return "Sorry that reply already exists";
                }
                else
                { 
                string Reply = Message.Remove(0, (Words[1].Length +1 ));
                Tuple<string,SteamID> Data = Tuple.Create<string, SteamID>(Reply, sender);
                    if (Words[1].StartsWith("/"))
                    { 
                        InstantChatReplies.Add( Words[1] , Data);
                        System.IO.File.WriteAllText(@InstantRepliesFile, JsonConvert.SerializeObject(InstantChatReplies));
                    }
                    else
                    {
                        InstantChatReplies.Add("/" + Words[1], Data);
                        System.IO.File.WriteAllText(@InstantRepliesFile, JsonConvert.SerializeObject(InstantChatReplies));
                    }
                    return "Set reply for: " + Words[1] + " which is: " + Reply;
                }
            }
            
            if (DoesMessageStartWith(Words[0], ChatCommandsArray["ReplyCheck"].Item2) & Words.Length > 1)
            {
                foreach (KeyValuePair<string, Tuple<string, SteamID>> Entry in InstantChatReplies)
                {
                    if ((Entry.Key == Words[1]) )
                    {
                        return "Set by: " + Entry.Value.Item2.ToString() + " (" + Bot.SteamFriends.GetFriendPersonaName(Entry.Value.Item2) + ")";

                    }
                }

            }
           

            foreach (KeyValuePair<string, Tuple<string,SteamID>> Entry in InstantChatReplies) //TODO Disable autocorrections
            {
                if (FullMessage.StartsWith(Entry.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return Entry.Value.Item1;
                }
            }
            return null;
        }
        /// <summary>
        /// Checks if the value is within the array
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Comparison"></param>
        /// <returns></returns>

        public static bool DoesMessageStartWith(string Message, List<string> Comparison)
        {

            foreach (string CommandWord in Comparison)
            {
                if (Message.StartsWith(CommandWord, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;

        }
        /// <summary>
        /// Adds a map to the database
        /// </summary>
        public string ImpEntry(string map, string downloadurl, string notes, SteamID sender)
        {
            if (notes == null)
            {
                notes = "No Notes";
            }
            //Deserialises the current map list
            string response = "Failed to add the map to the list";
            Dictionary<string, Tuple<string, SteamID, string, bool>> entrydata = Maplist;
            if (Maplist == null) {
                Log.Interface("There was an error, here is the map file before it's wiped:" + System.IO.File.ReadAllText(@MapStoragePath));
            }
            if (entrydata.ContainsKey(map)) { //if it already exists, it deletes it so it can update the data
                response = "Error, the entry already exists! Please remove the existing entry";
            } else {
                //Adds the entry
                entrydata.Add(map, new Tuple<string, SteamID, string, bool>(downloadurl, sender, notes, UploadCheck(map)));
                //Saves the data
                Maplist = entrydata;
                response = "Added: " + map;
            }
            System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(entrydata));
            SpreadsheetSync = true;
            return response;
        }
        /// <summary>
        /// Updates a map in the maplist, and keeps the position in the array
        /// </summary>
        /// <param name="maptochange"></param>
        /// <param name="map"></param>
        /// <param name="downloadurl"></param>
        /// <param name="notes"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public string ImpEntryUpdate(string maptochange, string map, string downloadurl, string notes, SteamID sender)
        {
            Log.Interface("Reached Here");

            if (notes == null)
            {
                notes = "No Notes";
            }
            int EntryCount = 0;

            if (admincheck(sender) == true | sender == Maplist[maptochange].Item2)
            {

                foreach (KeyValuePair<string, Tuple<string, SteamID, string, bool>> entry in Maplist)
                {
                    EntryCount = EntryCount + 1;

                    if (entry.Key == maptochange)
                    {
                        UpdateEntryExecute(EntryCount , maptochange ,  map , downloadurl, notes, sender);
                        return "Map has been updated";
                    }
                }
                
            }

            return "The entry was not found";

        }

        public void UpdateEntryExecute (int EntryCount , string maptochange, string map, string downloadurl, string notes, SteamID sender)
        {
            Dictionary<string, Tuple<string, SteamID, string, bool>> NewMaplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();
            foreach (KeyValuePair<string, Tuple<string, SteamID, string, bool>> OldMaplistEntry in Maplist)
            {
                if (NewMaplist.Count() + 1 != EntryCount)
                {
                    NewMaplist.Add(OldMaplistEntry.Key, OldMaplistEntry.Value);
                }
                else
                {
                    NewMaplist.Add(map, new Tuple<string, SteamID, string, bool>(downloadurl, sender, notes, UploadCheck(map)));
                    NewMaplist.Add(OldMaplistEntry.Key, OldMaplistEntry.Value);
                }
                Maplist = NewMaplist;
                Maplist.Remove(maptochange);
                System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(Maplist));
                SpreadsheetSync = true;
            }
        }

        /// <summary>
        /// Removes specified map from the database.
        /// Checks if the user is an admin or the setter
        /// </summary>
        public Tuple<string,SteamID> ImpRemove (string map , SteamID sender , bool ServerRemove, string DeletionReason)
		{
			Dictionary<string,Tuple<string,SteamID,string,bool> > NewMaplist = new Dictionary<string, Tuple<string, SteamID,string,bool>>();
			string removed = "The map was not found or you do not have sufficient privileges"; 
			SteamID userremoved = 0;
			foreach (var item in Maplist) {
				//TODO DEBUG
				if (item.Key == map && (admincheck (sender) || sender == item.Value.Item2 || ServerRemove )) {
					removed = map;
					userremoved = item.Value.Item2;
					SpreadsheetSync = true;
                    if (DeletionReason != null)
                    {
                        Bot.SteamFriends.SendChatMessage(item.Value.Item2, EChatEntryType.ChatMsg, "Hi, your map: " + item.Key + " was removed from the map list, reason given:" + DeletionReason);
                    }
                } else {
					NewMaplist.Add (item.Key, item.Value);
				}
			}
           
			System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(NewMaplist));
			Tuple<string,SteamID> RemoveInformation = new Tuple<string,SteamID> (removed, userremoved);
            Maplist = NewMaplist;
			return RemoveInformation ;
			{
			}
		}


        public string GetSteamIDFromUrl (string url , bool HumanReadable)
        {
           WebClient client = new WebClient();
           string SteamPage = client.DownloadString(url);

            
                string[] SteamIDPreCut = SteamPage.Split(new string[] { "steamid\":\"" }, StringSplitOptions.None);
            
            string[] SteamIDReturn = SteamIDPreCut[1].Split(new string[] { "\"" }, StringSplitOptions.None);
            Log.Interface(SteamIDReturn[0]);

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
        public bool UploadCheck(string Mapname){
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
		///<summary> Checks if the given STEAMID is an admin in the database</summary>
		public bool admincheck(SteamID sender)
		{
			if (UserDatabase.ContainsKey (sender.ToString ())) { //If the STEAMID is in the dictionary
				string Key = sender.ToString (); 
				EClanPermission UserPermissions = UserDatabase [Key]; //It will get the permissions value
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
        public string AdvancedGoogleSearch(string searchquery, string url, SteamID chatid)
        {
            if (OnlineSearch.StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                WebClient client = new WebClient();
                var search = client.DownloadString("https://www.googleapis.com/customsearch/v1?q=" + searchquery + "&cx=" + CX + "&siteSearch=" + url + "&key=" + APIKEY);
                var obj = JObject.Parse(search);
                var info = (string)obj["items"][0]["link"];
                Log.Interface(info.ToString());
                return info;
            }
            return null;
        }
		/// <summary>
		/// Checks for RSS feed updates, and posts on action
		/// </summary>
		public void RSSTracker (object sender, EventArgs e)
		{
			RSSTick.Close ();
			int count = 0;
			var reader = new FeedReader();
			foreach (string item in Feeds) 
			{
				var FeedItems = reader.RetrieveFeed(item);
				if ( ( 
                    (StoredFeeditems[count] != null) & 
                    (FeedItems != null) & 
                    (FeedItems.FirstOrDefault().Title.ToString() != StoredFeeditems[count])
                    & (EnableRSS = true)
                    )) 
                    {
                    Log.Interface(FeedItems.FirstOrDefault().Uri.ToString());
					Bot.SteamFriends.SendChatRoomMessage (Groupchat, EChatEntryType.ChatMsg, FeedItems.FirstOrDefault ().Title.ToString () + " " + FeedItems.FirstOrDefault ().Uri.ToString ());
					}
                StoredFeeditems[count] = FeedItems.FirstOrDefault().Title.ToString();
                count = count + 1; 
			}
			RSSTimer ();
		}
		/// <summary>
		/// Updates the online spreadsheet according the maps file
		/// </summary>
		/// 
		public void SheetSync (bool ForceSync)
		{
			Bot.SteamFriends.SetPersonaName ("[" + Maplist.Count.ToString() + "] " + Bot.DisplayName );
			
			if ((OnlineSync.StartsWith ("true", StringComparison.OrdinalIgnoreCase) && !SyncRunning) || ForceSync) {
				SyncRunning = true;
				//Log.Interface ("Beginning Sync");
				OAuth2Parameters parameters = new OAuth2Parameters ();
				parameters.ClientId = CLIENT_ID;
				parameters.ClientSecret = CLIENT_SECRET;
				parameters.RedirectUri = REDIRECT_URI;
				parameters.Scope = SCOPE;
				parameters.AccessType = "offline";
				parameters.RefreshToken = GoogleAPI;
				OAuthUtil.RefreshAccessToken (parameters);
				string accessToken = parameters.AccessToken;

                GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, IntegrationName, parameters);
                SpreadsheetsService service = new SpreadsheetsService (IntegrationName);
                service.RequestFactory = requestFactory;
                SpreadsheetQuery query = new SpreadsheetQuery(SpreadSheetURI);
                SpreadsheetFeed feed = service.Query(query);
                SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];
                WorksheetFeed wsFeed = spreadsheet.Worksheets;
                WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries[0];
             
                worksheet.Cols = 5;
                worksheet.Rows = Convert.ToUInt32(Maplist.Count + 1);

                worksheet.Update();

                CellQuery cellQuery = new CellQuery(worksheet.CellFeedLink);
                cellQuery.ReturnEmpty = ReturnEmptyCells.yes;
                CellFeed cellFeed = service.Query(cellQuery);
                CellFeed batchRequest = new CellFeed(cellQuery.Uri, service);

                int Entries = 1;

                foreach (var item in Maplist)
                {
                    Entries = Entries + 1;
                    foreach (CellEntry cell in cellFeed.Entries)
                    {
                        if (cell.Title.Text == "A" + Entries.ToString())
                        {
                            cell.InputValue = item.Key;
                        }
                        if (cell.Title.Text == "B" + Entries.ToString())
                        {
                            cell.InputValue = item.Value.Item1;
                            
                        }
                        if (cell.Title.Text == "C" + Entries.ToString())
                        {
                            cell.InputValue = item.Value.Item2.ToString();
                            
                        }
                        if (cell.Title.Text == "D" + Entries.ToString())
                        {
                            cell.InputValue = item.Value.Item3.ToString();
                            
                        }
                        if (cell.Title.Text == "E" + Entries.ToString())
                        {
                            cell.InputValue = item.Value.Item4.ToString();
                        }   
                    }
                }

                cellFeed.Publish();
                CellFeed batchResponse = (CellFeed)service.Batch(batchRequest, new Uri(cellFeed.Batch));
               // Log.Interface("Completed Sync");
				SyncRunning = false;

			} else if (OnlineSync.StartsWith ("true", StringComparison.OrdinalIgnoreCase)){
				SpreadsheetSync = true;
			}
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


        public override void OnTradeClose()
        {
            base.OnTradeClose();
        }

    }
}

