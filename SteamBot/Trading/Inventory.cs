using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SteamBot.Trading
{
    public class Inventory
    {

        /// <summary>
        /// The number of slots in the backpack.
        /// </summary>
        public ulong NumSlots
        {
            get
            {
                return inventory.BackpackSlots;
            }
        }

        /// <summary>
        /// The items in the backpack.
        /// </summary>
        public Item[] Items
        {
            get
            {
                return inventory.Items;
            }
        }

        /// <summary>
        /// Whether or not the backpack is private.
        /// </summary>
        public bool IsPrivate
        {
            get
            {
                return inventory.Status == 15;
            }
        }

        public Result inventory;

        public Inventory(ulong steamId, int appId, string apiKey)
        {
            Web web = new Web
            {
                Domain = "api.steampowered.com",
                Scheme = "http"
            };
            string response = web.Do(String.Format(
                "/IEconItems_{0}/GetPlayerItems/v0001/?key={1}&steamid={2}",
                appId, apiKey, steamId));
            Response inventoryResponse = JsonConvert.DeserializeObject<Response>(response);

            HandleInventoryResult(inventoryResponse);
        }

        public Inventory(Response apiInventory)
        {
            HandleInventoryResult(apiInventory);
        }

        void HandleInventoryResult(Response result)
        {
            inventory = result.Result;
        }

        /// <summary>
        /// Grab an item matching the specified id.
        /// </summary>
        /// <param name="id">The id to match.</param>
        /// <returns>The item if it can find one, null otherwise.</returns>
        public Item GetItem(ulong id)
        {
            return (from item in Items where item.Id == id select item).First();
        }

        /// <summary>
        /// Grab items matching the specified defindex.
        /// </summary>
        /// <param name="defindex">The defindex to match.</param>
        /// <returns>The list of items matching the defindex.</returns>
        public List<Item> GetItemByDefindex(int defindex)
        {
            return (from item in Items where item.Defindex == defindex select item).ToList();
        }

        #region JSON Responses
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

            public Schema.Item GetSchemaItem(Schema schema)
            {
                return (from items in schema.itemSchema.Items where items.Defindex == Defindex select items).First();
            }
        }

        public class ItemAttribute
        {
            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }

        public class Result
        {
            [JsonProperty("status")]
            public int Status { get; set; }

            [JsonProperty("num_backpack_slots")]
            public uint BackpackSlots { get; set; }

            [JsonProperty("items")]
            public Item[] Items { get; set; }
        }

        public class Response
        {
            [JsonProperty("result")]
            public Result Result { get; set; }
        }
        #endregion
    }
}
