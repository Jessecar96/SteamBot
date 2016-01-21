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
    public class UserDatabaseHandler
    {
        public static Dictionary<string, EClanPermission> UserDatabase = UserDatabaseRetrieve(UserDatabaseFile);
        public static string UserDatabaseFile = "users.json";
        public static Dictionary<string, EClanPermission> UserDatabaseRetrieve(string UserDatabase)
        { //TODO have this load with the bot

            if (!File.Exists(UserDatabase))
            {
                System.IO.File.WriteAllText(@UserDatabase, JsonConvert.SerializeObject(new Dictionary<string, EClanPermission>()));
                Dictionary<string, EClanPermission> UserDatabaseData = new Dictionary<string, EClanPermission>();
                return UserDatabaseData;
            }
            return JsonConvert.DeserializeObject<Dictionary<string, EClanPermission>>(System.IO.File.ReadAllText(@UserDatabase));
        }

        ///<summary> Checks if the given STEAMID is an admin in the database</summary>
		public static bool admincheck(SteamID sender)
        {
            if (UserDatabase.ContainsKey(sender.ToString()))
            { //If the STEAMID is in the dictionary
                string Key = sender.ToString();
                EClanPermission UserPermissions = UserDatabase[Key]; //It will get the permissions value
                if ((UserPermissions & EClanPermission.OwnerOfficerModerator) > 0) //Checks if it has sufficient privilages
                {
                    return true; //If there's sufficient privilages, it'll return true
                }
            }
            return false; //If there is no entry in the database, or there aren't sufficient privalages, it'll return false
        }
        public static string BanListFilePath = "Banlist.json";

        public static Dictionary<string, int> BanList = BanListFile(BanListFilePath);

        public static Dictionary<string, int> BanListFile(string BanListFilePath)
        {
            if (File.Exists(BanListFilePath))

            {
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(System.IO.File.ReadAllText(@BanListFilePath));
            }
            else {
                Dictionary<string, int> EmptyBanListFile = new Dictionary<string, int>();
                System.IO.File.WriteAllText(@GroupChatHandler.MapStoragePath, JsonConvert.SerializeObject(EmptyBanListFile));
                return EmptyBanListFile;
            }
        }
    }
}
