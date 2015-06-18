using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamAPI
{
    public class TF2Inventory : Inventory
    {
        /// <summary>
        /// Fetches the inventory for the given Steam ID using the Steam API.
        /// </summary>
        /// <returns>The give users inventory.</returns>
        /// <param name='steamId'>Steam identifier.</param>
        /// <param name='apiKey'>The needed Steam API key.</param>
        /// <param name="steamWeb">The SteamWeb instance for this Bot</param>
        public static TF2Inventory FetchInventory (ulong steamId, string apiKey, SteamWeb steamWeb)
        {
            var url = "http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=" + apiKey + "&steamid=" + steamId;
            string response = steamWeb.Fetch (url, "GET", null, false);
            InventoryResponse result = JsonConvert.DeserializeObject<InventoryResponse>(response);
            return new TF2Inventory(result.result) as TF2Inventory;
        }

        /// <summary>
        /// Gets the inventory for the given Steam ID using the Steam Community website.
        /// </summary>
        /// <returns>The inventory for the given user. </returns>
        /// <param name='steamid'>The Steam identifier. </param>
        /// <param name="steamWeb">The SteamWeb instance for this Bot</param>
        public static dynamic GetInventory(SteamID steamid, SteamWeb steamWeb)
        {
            string url = String.Format (
                "http://steamcommunity.com/profiles/{0}/inventory/json/440/2/?trading=1",
                steamid.ConvertToUInt64 ()
            );
            
            try
            {
                string response = steamWeb.Fetch (url, "GET");
                return JsonConvert.DeserializeObject (response);
            }
            catch (Exception)
            {
                return JsonConvert.DeserializeObject ("{\"success\":\"false\"}");
            }
        }

        public uint NumSlots { get; set; }

        protected TF2Inventory(InventoryResult apiInventory)
            : base(apiInventory)
        {
            
        }

        /// <summary>
        /// Check to see if user is Free to play
        /// </summary>
        /// <returns>Yes or no</returns>
        public bool IsFreeToPlay()
        {
            return this.NumSlots % 100 == 50;
        }        

        public class TF2Item : Inventory.Item
        {
            public int AppId = 440;
            public long ContextId = 2;
        }
    }
}

