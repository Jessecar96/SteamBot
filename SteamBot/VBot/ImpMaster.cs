using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.IO;
using Newtonsoft.Json;

namespace SteamBot
{
    public class ImpMaster
    {

        //public static string MapStoragePath = GroupChatHandler.groupchatsettings["MapStoragePath"];

        public static string MapStoragePath { get; set; }

        bool SpreadsheetSync { get; set; }

        public static SpreadSheetSyncDelegate SpreadSheetSyncDelegatePassedMethod { get; set; }

        public delegate bool SpreadSheetSyncDelegate(bool UpdateSheet);

        public static bool SpreadSheetSyncrhonise(bool UpdateSheet, SpreadSheetSyncDelegate SpreadSheetSynchroniser)
        {
            return SpreadSheetSynchroniser(UpdateSheet);
        }


        public static FileUploadCheckDelegate FileUploadCheckPassedMethod { get; set; }

        public delegate bool FileUploadCheckDelegate(string map);

        public static bool FileUploadCheck(string map, FileUploadCheckDelegate UploadChecker)
        {
            return UploadChecker(map);
        }



        public static AdminVerifyDelegate AdminVerifyPassedMethod { get; set; }

        public delegate bool AdminVerifyDelegate(SteamID User);

        public static bool AdminVerification(SteamID user, AdminVerifyDelegate AdminVerify)
        {
            return AdminVerify(user);
        }


        
        

        public static Dictionary<string, Tuple<string, SteamID, string, bool>> Maplist = Maplistfile(MapStoragePath);

        public static Dictionary<string, Tuple<string, SteamID, string, bool>> Maplistfile(string MapStoragePath)
        {
            if (File.Exists(MapStoragePath))

            {
                return JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, SteamID, string, bool>>>(System.IO.File.ReadAllText(@MapStoragePath));
            }
            else {
                Dictionary<string, Tuple<string, SteamID, string, bool>> EmptyMaplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();
                System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(EmptyMaplist));
                return EmptyMaplist;
            }
        }

        public static Dictionary<string, Tuple<string, SteamID, string, bool>> GetMapList ()
        {
            if (File.Exists(MapStoragePath))

            {
                return JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, SteamID, string, bool>>>(System.IO.File.ReadAllText(@MapStoragePath));
            }
            return null;
        }

        /// <summary>
        /// Adds a map to the database
        /// </summary>
        public static string ImpEntry(string map, string downloadurl, string notes, SteamID sender)
        {
            if (notes == null)
            {
                notes = "No Notes";
            }
            //Deserialises the current map list
            string response = "Failed to add the map to the list";
            Dictionary<string, Tuple<string, SteamID, string, bool>> entrydata = Maplist;
            if (Maplist == null)
            {
               // Log.Interface("There was an error, here is the map file before it's wiped:" + System.IO.File.ReadAllText(@MapStoragePath));
            }
            if (entrydata.ContainsKey(map))
            { //if it already exists, it deletes it so it can update the data
                response = "Error, the entry already exists! Please remove the existing entry";
            }
            else {
                //Adds the entry
                entrydata.Add(map, new Tuple<string, SteamID, string, bool>(downloadurl, sender, notes, FileUploadCheck(map,FileUploadCheckPassedMethod)));
               
                //Saves the data
                Maplist = entrydata;
                response = "Added: " + map;
            }
            System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(entrydata));

            //GroupChatHandler.SpreadsheetSync = true;
            SpreadSheetSyncrhonise(true,SpreadSheetSyncDelegatePassedMethod);


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
        public static string ImpEntryUpdate(string maptochange, string map, string downloadurl, string notes, SteamID sender)
        {
            if (notes == null)
            {
                notes = "No Notes";
            }
            int EntryCount = 0;

            if (AdminVerification(sender, AdminVerifyPassedMethod) == true | sender == Maplist[maptochange].Item2)
            {
                foreach (KeyValuePair<string, Tuple<string, SteamID, string, bool>> entry in Maplist)
                {
                    EntryCount = EntryCount + 1;

                    if (entry.Key == maptochange)
                    {
                        UpdateEntryExecute(EntryCount, maptochange, map, downloadurl, notes, sender);
                        return "Map has been updated";
                    }
                }
            }
            return "The entry was not found";
        }

        public static void UpdateEntryExecute(int EntryCount, string maptochange, string map, string downloadurl, string notes, SteamID sender)
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
                    NewMaplist.Add(map, new Tuple<string, SteamID, string, bool>(downloadurl, sender, notes, FileUploadCheck(map, FileUploadCheckPassedMethod)));
                    NewMaplist.Add(OldMaplistEntry.Key, OldMaplistEntry.Value);
                }
                Maplist = NewMaplist;
                Maplist.Remove(maptochange);
                System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(Maplist));

                //GroupChatHandler.SpreadsheetSync = true;
                SpreadSheetSyncrhonise(true, SpreadSheetSyncDelegatePassedMethod);
            }
        }
        public static void WipeAllMaps ()
        {

            Dictionary<string, Tuple<string, SteamID, string, bool>> Maplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();

            System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(Maplist));

            Maplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();

            SpreadSheetSyncrhonise(true, SpreadSheetSyncDelegatePassedMethod);

            return ;
        }

        /// <summary>
        /// Removes specified map from the database.
        /// Checks if the user is an admin or the setter
        /// </summary>
        public static Tuple<string, SteamID> ImpRemove(string map, SteamID sender, bool ServerRemove, string DeletionReason)
        {
            Dictionary<string, Tuple<string, SteamID, string, bool>> NewMaplist = new Dictionary<string, Tuple<string, SteamID, string, bool>>();
            string removed = "The map was not found or you do not have sufficient privileges";
            SteamID userremoved = 0;

            foreach (var item in Maplist)
            {
                //TODO DEBUG
                if (item.Key == map && (AdminVerification(sender, AdminVerifyPassedMethod) || sender == item.Value.Item2 || ServerRemove))
                {
                    removed = map;
                    userremoved = item.Value.Item2;
                    //GroupChatHandler.SpreadsheetSync = true;
                    SpreadSheetSyncrhonise(true, SpreadSheetSyncDelegatePassedMethod);

                    if (DeletionReason != null)
                    {
                        //Bot.SteamFriends.SendChatMessage(item.Value.Item2, EChatEntryType.ChatMsg, "Hi, your map: " + item.Key + " was removed from the map list, reason given:" + DeletionReason);
                    }
                }
                else {
                    NewMaplist.Add(item.Key, item.Value);
                }
            }
            System.IO.File.WriteAllText(@MapStoragePath, JsonConvert.SerializeObject(NewMaplist));

            Tuple<string, SteamID> RemoveInformation = new Tuple<string, SteamID>(removed, userremoved);

            Maplist = NewMaplist;

            return RemoveInformation;
        }
      
    }
}
