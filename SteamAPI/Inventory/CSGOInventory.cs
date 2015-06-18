using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamAPI
{
    public class CSGOInventory : Inventory
    {
        /// <summary>
        /// Fetches the inventory for the given Steam ID using the Steam API.
        /// </summary>
        /// <returns>The give users inventory.</returns>
        /// <param name='steamId'>Steam identifier.</param>
        /// <param name='apiKey'>The needed Steam API key.</param>
        /// <param name="steamWeb">The SteamWeb instance for this Bot</param>
        public static CSGOInventory FetchInventory(ulong steamId, string apiKey, SteamWeb steamWeb)
        {
            var url = "http://api.steampowered.com/IEconItems_730/GetPlayerItems/v0001/?key=" + apiKey + "&steamid=" + steamId;
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    string response = steamWeb.Fetch(url, "GET", null, false);
                    InventoryResponse result = JsonConvert.DeserializeObject<InventoryResponse>(response);
                    if (result.result != null)
                    {
                        return new CSGOInventory(result.result) as CSGOInventory;
                    }
                }
                catch (System.Net.WebException we)
                {
                    
                }                
            }
            throw new InventoryException("Failed to load CSGO inventory.");
        }

        /// <summary>
        /// Gets the inventory for the given Steam ID using the Steam Community website.
        /// </summary>
        /// <returns>The inventory for the given user. </returns>
        /// <param name='steamid'>The Steam identifier. </param>
        /// <param name="steamWeb">The SteamWeb instance for this Bot</param>
        public static dynamic GetInventory(SteamID steamid, SteamWeb steamWeb)
        {
            string url = String.Format(
                "http://steamcommunity.com/profiles/{0}/inventory/json/730/2/?trading=1",
                steamid.ConvertToUInt64()
            );

            try
            {
                string response = steamWeb.Fetch(url, "GET");
                return JsonConvert.DeserializeObject(response);
            }
            catch (Exception)
            {
                return JsonConvert.DeserializeObject("{\"success\":\"false\"}");
            }
        }

        protected CSGOInventory(InventoryResult apiInventory)
            : base(apiInventory)
        {

        }

        public class CSGOItem : Inventory.Item
        {
            public int AppId = 730;
            public long ContextId = 2;
        }        
    }
}

