using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade
{
    public class Dota2Inventory
    {
        /// <summary>
        /// Fetches the inventory for the given Steam ID using the Steam API.
        /// </summary>
        /// <returns>The give users inventory.</returns>
        /// <param name='steamId'>Steam identifier.</param>
        /// <param name='apiKey'>The needed Steam API key.</param>
        public static Dota2Inventory FetchInventory(ulong steamId, string apiKey)
        {
            var url = "http://api.steampowered.com/IEconItems_570/GetPlayerItems/v0001/?key=" + apiKey + "&steamid=" + steamId;
            string response = SteamWeb.Fetch(url, "GET", null, null, false);
            InventoryResponse result = JsonConvert.DeserializeObject<InventoryResponse>(response);
            return new Dota2Inventory(result.result);
        }
        
        public uint NumSlots { get; set; }
        public Item[] Items { get; set; }
        public bool IsPrivate { get; private set; }
        public bool IsGood { get; private set; }

        protected Dota2Inventory(InventoryResult apiInventory)
        {
            NumSlots = apiInventory.num_backpack_slots;
            Items = apiInventory.items;
            IsPrivate = (apiInventory.status == "15");
            IsGood = (apiInventory.status == "1");
        }

        public Item GetItem(ulong id)
        {
            return (Items == null ? null : Items.FirstOrDefault(item => item.Id == id));
        }

        public List<Item> GetItemsByDefindex(int defindex)
        {
            return Items.Where(item => item.Defindex == defindex).ToList();
        }

        public class Item
        {
            public int AppId = 570;
            public long ContextId = 2;

            [JsonProperty("id")]
            public ulong Id { get; set; }

            [JsonProperty("original_id")]
            public ulong OriginalId { get; set; }

            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("level")]
            public byte Level { get; set; }

            [JsonProperty("quality")]
            public int Quality { get; set; }

            [JsonProperty("inventory")]
            public ulong InventoryId { get; set; }

            [JsonProperty("quantity")]
            public int Quantity { get; set; }

            [JsonProperty("flag_cannot_craft")]
            public bool IsNotCraftable { get; set; }

            [JsonProperty("flag_cannot_trade")]
            public bool IsNotTradeable { get; set; }

            [JsonProperty("attributes")]
            public ItemAttribute[] Attributes { get; set; }
        }

        public class ItemAttribute
        {
            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("float_value")]
            public float FloatValue { get; set; }
        }

        protected class InventoryResult
        {
            public string status { get; set; }

            public uint num_backpack_slots { get; set; }

            public Item[] items { get; set; }
        }

        protected class InventoryResponse
        {
            public InventoryResult result;
        }

    }
}

