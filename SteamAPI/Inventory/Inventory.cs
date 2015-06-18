using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SteamAPI
{
    public class Inventory
    {
        public Item[] Items { get; set; }
        public bool IsPrivate { get; private set; }
        public bool IsGood { get; private set; }

        public Inventory()
        {
            IsGood = false;
        }

        protected Inventory (InventoryResult apiInventory)
        {            
            Items = apiInventory.items;
            IsPrivate = (apiInventory.status == "15");
            IsGood = (apiInventory.status == "1");
        }

        public Item GetItem(ulong id)
        {
            // Check for Private Inventory
            if (this.IsPrivate)
                throw new InventoryException("Unable to access Inventory: Inventory is Private!");

            return (Items == null ? null : Items.FirstOrDefault(item => item.Id == id));
        }

        public Item GetItemByOriginalId(ulong originalId)
        {
            // Check for Private Inventory
            if (this.IsPrivate)
                throw new InventoryException("Unable to access Inventory: Inventory is Private!");

            return (Items == null ? null : Items.FirstOrDefault(item => item.OriginalId == originalId));
        }

        public List<Item> GetItemsByDefindex(int defindex)
        {
            // Check for Private Inventory
            if (this.IsPrivate)
                throw new InventoryException("Unable to access Inventory: Inventory is Private!");

            return Items.Where(item => item.Defindex == defindex).ToList();
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
            public int Quality { get; set; }

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

            [JsonProperty("contained_item")]
            public Item ContainedItem { get; set; }
        }

        public class ItemAttribute
        {
            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("float_value")]
            public float FloatValue { get; set; }

            [JsonProperty("account_info")]
            public AccountInfo AccountInfo { get; set; }
        }

        public class AccountInfo
        {
            [JsonProperty("steamid")]
            public ulong SteamID { get; set; }

            [JsonProperty("personaname")]
            public string PersonaName { get; set; }
        }

        protected class InventoryResult
        {
            public string status { get; set; }

            public Item[] items { get; set; }

            public uint num_backpack_slots { get; set; }
        }

        protected class InventoryResponse
        {
            public InventoryResult result;
        }

        public class InventoryException : Exception
        {
            public InventoryException()
            {

            }

            public InventoryException(string message)
                : base(message)
            {

            }

            public InventoryException(string message, Exception inner)
                : base(message, inner)
            {

            }
        }
    }
}
