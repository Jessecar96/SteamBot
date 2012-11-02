using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamBot
{
    public class Inventory
    {
        public static Inventory FetchInventory(ulong steamId, string apiKey)
        {
            var url = "http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=" + apiKey + "&steamid=" + steamId;
            string response = SteamWeb.Fetch(url, "GET", null, null, false);
            InventoryResponse result = JsonConvert.DeserializeObject<InventoryResponse>(response);
            return new Inventory(result.result);
        }

        public uint NumSlots { get; set; }
        public Item[] Items { get; set; }

        protected Inventory(InventoryResult apiInventory)
        {
            NumSlots = apiInventory.num_backpack_slots;
            Items = apiInventory.items;
        }

        public Item GetItem(ulong id)
        {
            foreach (Item item in Items)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }
            return null;
        }

        public List<Item> GetItemsByDefindex(int defindex)
        {
            var items = new List<Item>();
            foreach (Item item in Items)
            {
                if (item.Defindex == defindex)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        public class Item
        {
            [JsonProperty("id")]
            public ulong Id { get; set; }

            [JsonProperty("original_id")]
            public ulong OriginalId { get; set; }

            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("level")]
            public byte Level { get; set; }

            [JsonProperty("quality")]
            public string Quality { get; set; }

            [JsonProperty("pos")]
            public int Position { get; set; }

            [JsonProperty("quantity")]
            public int RemainingUses { get; set; }

            [JsonProperty("origin")]
            public int Origin { get; set; }

            [JsonProperty("custom_name")]
            public string CustomName { get; set; }

            [JsonProperty("custom_desc")]
            public string CustomDescription { get; set; }

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

