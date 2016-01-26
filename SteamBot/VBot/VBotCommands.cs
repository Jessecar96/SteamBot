using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.Internal;
using SteamTrade;
using SteamTrade.TradeWebAPI;
using SteamKit2;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace SteamBot
{
    public class VBotCommands
    {
        public static string Mapstoragepath2 = GroupChatHandler.groupchatsettings["MapStoragePath"];
        static string MapStoragePath = GroupChatHandler.groupchatsettings["MapStoragePath"];

        static string ChatCommandsFilePath = "ChatCommands.Json";
        static ImpMaster ImpMasterData { get;  set; }
        public UserDatabaseHandler UserDatabase { get; private set; }

        public static Tuple<string, string, string, Int32>[] Servers = GroupChatHandler.ExtraSettingsData.Servers;



        static Dictionary<string, Tuple<string,string[]>> ChatCommands = JsonConvert.DeserializeObject<Dictionary<string, Tuple<string,string[]>>>(System.IO.File.ReadAllText(@ChatCommandsFilePath));
        

        public static void CommandPreProcessing (SteamID sender , SteamID ChatID , string FullMessage , bool InChatroom )
        {
            
        }


        
        /// <summary>
        /// The commands that users can use by msg'ing the system. Returns a string with the appropriate responses
        /// </summary>
        /// <param name="chatID">ChatID of the chatroom</param>
        /// <param name="message">The message sent</param>
        public static string admincommands(SteamID sender, string FullMessage , Bot Bot)
        {
            FullMessage.Replace(@"\s+", " ");
            string[] Words = FullMessage.Split();

            string Message = FullMessage.Remove(0, Words[0].Length + (Words.Length > 1).GetHashCode());

            if (DoesMessageStartWith(Words[0], ChatCommands["Rejoin"].Item2))
                if (FullMessage.StartsWith("!ReJoin", StringComparison.OrdinalIgnoreCase))
            {
                Bot.SteamFriends.LeaveChat(new SteamID(GroupChatHandler.Groupchat));
                Bot.SteamFriends.JoinChat(new SteamID(GroupChatHandler.Groupchat));
            }
            if (Words[0].StartsWith("!Say", StringComparison.OrdinalIgnoreCase))
            {
                Bot.SteamFriends.SendChatRoomMessage(GroupChatHandler.Groupchat, EChatEntryType.ChatMsg, Message);
            }
            if(DoesMessageStartWith(Words[0], ChatCommands["SetMOTD"].Item2))
            {
                if (Message != null)
                {

                    if (BackgroundWork.MOTD != null)
                    {
                        return "There is currently a MOTD, please remove it first";
                    }
                    else
                    {
                        BackgroundWork.MOTDSetter = Bot.SteamFriends.GetFriendPersonaName(sender) + " " + sender;
                        BackgroundWork.MOTD = Message;
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
            if(DoesMessageStartWith(Words[0], ChatCommands["RemoveMOTD"].Item2))
            {
                BackgroundWork.MOTD = null;
                return "Removed MOTD";
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["ClearCommands"].Item2))
            {
                ImpMaster.WipeAllMaps();

                return "Wiped all Maps";
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["EnableSync"].Item2))
            {
                GroupChatHandler.OnlineSync = "true";
                GroupChatHandler.groupchatsettings.Remove("OnlineSync");
                GroupChatHandler.groupchatsettings.Add("OnlineSync", "true");
                System.IO.File.WriteAllText(@"ExtraSettings.json", JsonConvert.SerializeObject(GroupChatHandler.ExtraSettingsData));
                return "Enabled Sync";
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["DisableSync"].Item2))
            {
                GroupChatHandler.OnlineSync = "false";
                GroupChatHandler.groupchatsettings.Remove("OnlineSync");
                GroupChatHandler.groupchatsettings.Add("OnlineSync", "false");
                System.IO.File.WriteAllText(@"ExtraSettings.json", JsonConvert.SerializeObject(GroupChatHandler.ExtraSettingsData));
                return "Disabled Sync";
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["EnableRSS"].Item2))
            {
                GroupChatHandler.EnableRSS = true;
                return "Enabled RSS";
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["DisableRSS"].Item2))
            {
                GroupChatHandler.EnableRSS = false;
                return "Disabled RSS";
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["Rejoin"].Item2))
            {
                Bot.SteamFriends.LeaveChat(new SteamID(GroupChatHandler.Groupchat));
                Bot.SteamFriends.JoinChat(new SteamID(GroupChatHandler.Groupchat));
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["Unban"].Item2))
            {
                if (Words.Length > 1) ;
                string Userid = GroupChatHandler.GetSteamIDFromUrl(Words[1], true);
                if (UserDatabaseHandler.BanList.ContainsKey(Userid.ToString()))
                {
                    UserDatabaseHandler.BanList.Remove(Userid);
                    return "The ban has now been lifted";
                }
                else
                {
                    return "User is not banned";
                }
            }

            if (DoesMessageStartWith(Words[0], ChatCommands["Ban"].Item2))

            {
                

                if (Words.Length > 3)
                {
                    string[] Reason = Message.Split(new string[] { " " + Words[2] + " " }, StringSplitOptions.RemoveEmptyEntries);

                    SteamID Userid = new SteamID(GroupChatHandler.GetSteamIDFromUrl(Words[1], true));

                    if (UserDatabaseHandler.BanList.ContainsKey(Userid.ToString()))
                    {
                        return "This user has already been banned, their ban has " + UserDatabaseHandler.BanList[Userid.ToString()] + " remainig";
                    }
                    UserDatabaseHandler.BanList.Add(Userid.ToString(), int.Parse(Words[2]) * 24);
                    System.IO.File.WriteAllText(@UserDatabaseHandler.BanListFilePath, JsonConvert.SerializeObject(UserDatabaseHandler.BanList));

                    Bot.SteamFriends.SendChatMessage(Userid, EChatEntryType.ChatMsg, "You have been banned from using all bot features for " + Words[2] + "days. Reason given: " + Reason[1]);
                    return "Banned user:" + Userid.ToString() + " (" + Bot.SteamFriends.GetFriendPersonaName(Userid).ToString() + ") for: " + Words[2] + " days. Reason given: " + Reason[1];
                }
                else
                {
                    return "The command is: " + "!Ban" + " <url of user>, Duration in days, Reason";
                }
            }
            

            return null;
        }


        public bool DoesMessageStartWith(String[] Comparison)
        {
            return false;
        }

        /// <summary>
        /// The commands that users can use by msg'ing the system. Returns a string with the appropriate responses
        /// </summary>
        /// <param name="chatID">ChatID of the chatroom</param>
        /// <param name="sender">STEAMID of the sender</param>
        /// <param name="message">The message sent</param>
        public static string Chatcommands(SteamID chatID, SteamID sender, string FullMessage, Bot bot)
        {
            FullMessage.Replace(@"\s+", " ");
            string[] Words = FullMessage.Split(' ');
            string Message = FullMessage.Remove(0, Words[0].Length + (Words.Length > 1).GetHashCode()); ;
            if (Words.Length > 1)
            {
                Message = FullMessage.Remove(Words[0].Length + 1);  //TODO GET THIS PART FIXED
            }
            
            //base.OnChatRoomMessage(chatID, sender, message);

            bool rank = UserDatabaseHandler.admincheck(sender);

            foreach (KeyValuePair<string, string> Entry in GroupChatHandler.DataLOG) //TODO Disable autocorrections
            {
                if (Words[0].StartsWith(Entry.Key, StringComparison.OrdinalIgnoreCase))
                {
                  
                    return GroupChatHandler.AdvancedGoogleSearch(Message, Entry.Value, chatID);
                }
            }
            foreach (KeyValuePair<string, string> Entry in GroupChatHandler.InstantReplies) //TODO Disable autocorrections
            {

                if (FullMessage.StartsWith(Entry.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return Entry.Value;
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["SteamIDCommand"].Item2))
            {
                if (Words.Length > 0)
                {
                    return GroupChatHandler.GetSteamIDFromUrl(Words[1], true);
                }
                else {
                    return "URL is missing, please add a url";
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["MySteamIDCommand"].Item2))
            {
                return sender.ToString();
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["Rejoin"].Item2))
            {
                bot.SteamFriends.JoinChat(new SteamID(GroupChatHandler.Groupchat));
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["MOTDSetter"].Item2))
            {
                return BackgroundWork.MOTDSetter;
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["MOTDTick"].Item2))
            {
                return BackgroundWork.MOTDPosted.ToString();
            }

            if (DoesMessageStartWith(Words[0], ChatCommands["MOTD"].Item2))
            {
                return BackgroundWork.MOTD;
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["Sync"].Item2))
            {
                BackgroundWork.SheetSync(true);
                return null;
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["UploadCheckCommand"].Item2))
            {
                if (Words.Length > 0) {
                    return GroupChatHandler.UploadCheck(Words[1]).ToString();
                }
                else {
                    return "No map specified";
                }
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["DeleteCommands"].Item2))
            {
                if (Words.Length > 2)
                 
                {
                    string[] Reason = Message.Split(new string[] { Words[1] }, StringSplitOptions.None);
                    Tuple<string, SteamID> removed = ImpMaster.ImpRemove(Words[1], sender, false, Reason[1]);
                    return "Removed map: " + removed.Item1;
                }
                else
                {
                    Tuple<string, SteamID> removed = ImpMaster.ImpRemove(Words[1], sender, false, "(None)");
                    return "Removed map: " + removed.Item1;
                }
               
                
            }
            if (DoesMessageStartWith(Words[0], ChatCommands["ImpCommands"].Item2))
            { 
                
                if (Words.Length == 1) {
                    return "!add <mapname> <url> <notes> is the command. however if the map is uploaded you do not need to include the url";
                }
                int length = (Words.Length > 2).GetHashCode(); //Checks if there are more than 3 or more words
                int Uploaded = (GroupChatHandler.UploadCheck(Words[1])).GetHashCode(); //Checks if map is uploaded. Crashes if only one word //TODO FIX THAT
                string UploadStatus = "Uploaded"; //Sets a string, that'll remain unless altered
                if (length + Uploaded == 0) { //Checks if either test beforehand returned true
                    return "Make sure to include the download URL!";
                } else {
                    string[] notes = FullMessage.Split(new string[] { Words[2 - Uploaded] }, StringSplitOptions.None); //Splits by the 2nd word (the uploadurl) but if it's already uploaded, it'll split by the map instead 
                    if (Uploaded == 0) //If the map isn't uploaded, it'll set the upload status to the 3rd word (The URL)
                    {
                        UploadStatus = Words[2];
                    }
                    string status = ImpMaster.ImpEntry(Words[1], UploadStatus, notes[1], sender); //If there are no notes, but a map and url, this will crash.
                    GroupChatHandler.SpreadsheetSync = true;
                    return status;
                }

            }
            if (DoesMessageStartWith(Words[0], ChatCommands["Update"].Item2))
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
                    Uploaded = (GroupChatHandler.UploadCheck(FullMessageCutArray[0])).GetHashCode();
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
                    string status = ImpMaster.ImpEntryUpdate(Words[1], FullMessageCutArray[0], UploadStatus, notes[1], sender); //If there are no notes, but a map and url, this will crash.
                    GroupChatHandler.SpreadsheetSync = true;
                    return status;
                }

            }
            if (DoesMessageStartWith(Words[0], ChatCommands["MapCommands"].Item2))
            {


                Dictionary<string, Tuple<string, SteamID, string, bool>> Maplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();
                
           if (File.Exists(@MapStoragePath))

                {
                  Maplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>(JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, SteamID, string, bool>>>(System.IO.File.ReadAllText(@MapStoragePath)));
              }
             else
               {
                    System.IO.File.WriteAllText(@GroupChatHandler.MapStoragePath, JsonConvert.SerializeObject(Maplist));
              }
                
                string Maplisting = "";
                string DownloadListing = "";
                foreach (var item in Maplist)
              {
                    Maplisting = Maplisting + item.Key + " , ";
                    DownloadListing = DownloadListing + item.Value.Item1 + " , ";
              }
                
                bot.SteamFriends.SendChatMessage(sender, EChatEntryType.ChatMsg, DownloadListing);
                return Maplisting;
            }

            if (DoesMessageStartWith(Words[0], ChatCommands["HelpCommands"].Item2))
            {
                if (Words.Length > 1)
                {
                    foreach (KeyValuePair<string, Tuple<string, string[]>> Entry in ChatCommands)
                    {
                        if (Entry.Value.Item2.Contains(Words[1]))
                        {
                            return Entry.Key + ": " + Entry.Value.Item1 + " Commands: " + string.Join(", ", Entry.Value.Item2); ;
                        }

                    }
                }
                else
                {
                    return GroupChatHandler.HelpLink;
                }
            }



            foreach (Tuple<string, string, string, Int32> ServerAddress in Servers)
            {
                if (Words[0].StartsWith(ServerAddress.Item3, StringComparison.OrdinalIgnoreCase))
                {
                    Steam.Query.ServerInfoResult ServerData = SteamBot.BackgroundWork.ServerQuery(System.Net.IPAddress.Parse(ServerAddress.Item2), ServerAddress.Item4);
                    return ServerData.Map + " " + ServerData.Players + "/" + ServerData.MaxPlayers;
                }
            }
            return null;
        }

       public static bool DoesMessageStartWith (string Message, string[] Comparison)
        {
            
            foreach(string CommandWord in Comparison)
            {
                if (Message.StartsWith(CommandWord, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
                
        }

       
    }
}
