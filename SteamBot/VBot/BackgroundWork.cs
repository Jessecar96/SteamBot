using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SteamKit2;
using System.Net;
using SimpleFeedReader;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using Google.Apis.Customsearch;
using Google.Apis.Services;
using System.IO;
using Newtonsoft.Json;

namespace SteamBot
{
    class BackgroundWork
    {
        public static UserDatabaseHandler UserDatabase { get; set; }

        public static Tuple<string, string, string, Int32>[] Servers = GroupChatHandler.ExtraSettingsData.Servers;
        public static Bot Bot { get; private set; }
        public static ImpMaster ImpMaster { get; private set; }
        public static GroupChatHandler GroupChatHandler { get; private set; }

        public static string[] Feeds = GroupChatHandler.ExtraSettingsData.Feeds;
        public static string[] StoredFeeditems = new string[Feeds.Length];
        public static string[] PreviousData = new string[Servers.Length];
        public static bool SyncRunning = false;
        public static string MapStoragePath = GroupChatHandler.groupchatsettings["MapStoragePath"];

        //These are the variables for google stuff
        
        public static string SpreadSheetURI = "https://spreadsheets.google.com/feeds/spreadsheets/private/full/" + GroupChatHandler.groupchatsettings["SpreadSheetURI"];
        //These are the variables for google stuff

        

        private static Timer Tick;
        private static Timer MOTDTick;
        private static Timer RSSTick;
        static double interval = 5000;
        public static int GhostCheck = 120;
        public static int MOTDPosted = 0;
        static double MOTDHourInterval = 1;
        public static string MOTD = null;
        public static string MOTDSetter = null;

        /// <summary>
        /// Initialises the main timer
        /// </summary>
        public static void InitTimer()
        {
            Tick = new Timer();
            Tick.Elapsed += new ElapsedEventHandler(TickTasks);
            Tick.Interval = interval; // in miliseconds
            Tick.Start();
        }

        /// <summary>
        /// Initialises the Timer that RSS feeds will be checked on
        /// </summary>
        public static void RSSTimer()
        {
            RSSTick = new Timer();
            RSSTick.Elapsed += new ElapsedEventHandler(RSSTracker);
            RSSTick.Interval = 30000; // in miliseconds
            RSSTick.Start();
        }
        
        /// <summary>
        /// Initialises the MOTD timer
        /// </summary>
        public static void InitMOTDTimer()
        {
            MOTDTick = new Timer();
            MOTDTick.Elapsed += new ElapsedEventHandler(MOTDPost);
            MOTDTick.Interval = MOTDHourInterval * 1000 * 60 * 60; // in miliseconds TODO update this to formulate once a day
            MOTDTick.Start();
         

    }
        /// <summary>
        /// Tracks all maps in the server list and posts to the group when there's a map change
        /// </summary>
        public static void MapChangeTracker()
        {
            int count = 0;


            foreach (Tuple<string, string, string, Int32> ServerAddress in Servers)
            {
                Steam.Query.ServerInfoResult ServerData = ServerQuery(System.Net.IPAddress.Parse(ServerAddress.Item2), 27015);
                if ((ServerData.Map != PreviousData[count]) && ServerData.Players > 2)
                {
                    Tuple<string, SteamID> Mapremoval = ImpMaster.ImpRemove(ServerData.Map, 0, true, null);
                    Bot.SteamFriends.SendChatMessage(Mapremoval.Item2, EChatEntryType.ChatMsg, "Hi, your map: " + Mapremoval.Item1 + " is being played on the " + ServerAddress.Item1 + "!");
                    Bot.SteamFriends.SendChatRoomMessage(GroupChatHandler.Groupchat, EChatEntryType.ChatMsg, "Map changed to: " + ServerData.Map.ToString() + " on the " + ServerAddress.Item1 + " " + ServerData.Players + "/" + ServerData.MaxPlayers);

                    GroupChatHandler.SpreadsheetSync = true;
                }
                PreviousData[count] = ServerData.Map;
                count = count + 1;
            }


        }

        /// <summary>
        /// Posts the MOTD to the group chat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void MOTDPost(object sender, EventArgs e)
        {
            if ((MOTD == null) | (MOTDPosted >= 24))
            {
                MOTD = null;
              
                MOTDPosted = 0;
            }
            else
            {
                MOTDPosted = MOTDPosted + 1;
                Bot.SteamFriends.SetPersonaName("MOTD");
                Bot.SteamFriends.SendChatRoomMessage(GroupChatHandler.Groupchat, EChatEntryType.ChatMsg, MOTD);
                Bot.SteamFriends.SetPersonaName("[" + ImpMaster.Maplist.Count.ToString() + "] " + Bot.DisplayName);
            }
            foreach (KeyValuePair<string, int> Key in UserDatabaseHandler.BanList)
            {
                UserDatabaseHandler.BanList.Remove(Key.Key);
                UserDatabaseHandler.BanList.Add(Key.Key, Key.Value - 1);
            }
        }

        /// <summary>
        /// The Main Timer's method, executed per tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TickTasks(object sender, EventArgs e)
        {
            if (GroupChatHandler.SpreadsheetSync)
            {
                GroupChatHandler.SpreadsheetSync = false;
                SheetSync(false);
            }
            MapChangeTracker();
            GhostCheck = GhostCheck - 1;
            if (GhostCheck <= 1)
            {
                GhostCheck = 120;
                Bot.SteamFriends.LeaveChat(new SteamID(GroupChatHandler.Groupchat));
                Bot.SteamFriends.JoinChat(new SteamID(GroupChatHandler.Groupchat));
            }
        }
        /// <summary>
        /// Queries the server and returns the information
        /// </summary>
        /// <param name="ipadress">Ipadress that will be queried</param>
        /// <param name="port"> The port that will be used, typically 27015 </param> 
        public static Steam.Query.ServerInfoResult ServerQuery(System.Net.IPAddress ipaddress, Int32 port)
        {
            IPEndPoint ServerIP = new IPEndPoint(ipaddress, port);
            Steam.Query.Server Information = new Steam.Query.Server(ServerIP);
            Steam.Query.ServerInfoResult ServerInformation = Information.GetServerInfo().Result;
            return ServerInformation;
        }
        /// <summary>
		/// Checks for RSS feed updates, and posts on action
		/// </summary>
		public static void RSSTracker(object sender, EventArgs e)
        {
            RSSTick.Close();
            int count = 0;
            var reader = new FeedReader();
            foreach (string item in Feeds)
            {
                var FeedItems = reader.RetrieveFeed(item);
                if ((
                    (StoredFeeditems[count] != null) &
                    (FeedItems != null) &
                    (FeedItems.FirstOrDefault().Title.ToString() != StoredFeeditems[count])
                    & (GroupChatHandler.EnableRSS = true)
                    ))
                {
                   
                    Bot.SteamFriends.SendChatRoomMessage(GroupChatHandler.Groupchat, EChatEntryType.ChatMsg, FeedItems.FirstOrDefault().Title.ToString() + " " + FeedItems.FirstOrDefault().Uri.ToString());
                }
                StoredFeeditems[count] = FeedItems.FirstOrDefault().Title.ToString();
                count = count + 1;
            }
            RSSTimer();
        }

        /// <summary>
		/// Updates the online spreadsheet according the maps file
		/// </summary>
		/// 
		public static void SheetSync(bool ForceSync)
        {
            Bot.SteamFriends.SetPersonaName("[" + ImpMaster.Maplist.Count.ToString() + "] " + Bot.DisplayName);

            if ((GroupChatHandler.OnlineSync.StartsWith("true", StringComparison.OrdinalIgnoreCase) && !SyncRunning) || ForceSync)
            {
                SyncRunning = true;
                //Log.Interface ("Beginning Sync");
                OAuth2Parameters parameters = new OAuth2Parameters();
                parameters.ClientId = GroupChatHandler.CLIENT_ID;
                parameters.ClientSecret = GroupChatHandler.CLIENT_SECRET;
                parameters.RedirectUri = GroupChatHandler.REDIRECT_URI;
                parameters.Scope = GroupChatHandler.SCOPE;
                parameters.AccessType = "offline";
                parameters.RefreshToken = GroupChatHandler.GoogleAPI;
                OAuthUtil.RefreshAccessToken(parameters);
                string accessToken = parameters.AccessToken;

                GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, GroupChatHandler.IntegrationName, parameters);
                SpreadsheetsService service = new SpreadsheetsService(GroupChatHandler.IntegrationName);
                service.RequestFactory = requestFactory;
                SpreadsheetQuery query = new SpreadsheetQuery(SpreadSheetURI);
                SpreadsheetFeed feed = service.Query(query);
                SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];
                WorksheetFeed wsFeed = spreadsheet.Worksheets;
                WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries[0];

                worksheet.Cols = 5;
                worksheet.Rows = Convert.ToUInt32(ImpMaster.Maplist.Count + 1);

                worksheet.Update();

                CellQuery cellQuery = new CellQuery(worksheet.CellFeedLink);
                cellQuery.ReturnEmpty = ReturnEmptyCells.yes;
                CellFeed cellFeed = service.Query(cellQuery);
                CellFeed batchRequest = new CellFeed(cellQuery.Uri, service);

                int Entries = 1;

                foreach (var item in ImpMaster.Maplist)
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

            }
            else if (GroupChatHandler.OnlineSync.StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                GroupChatHandler.SpreadsheetSync = true;
            }
        }
    }
}
