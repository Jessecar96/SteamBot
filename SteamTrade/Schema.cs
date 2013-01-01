using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace SteamTrade
{
    public class Schema
    {

        public static Schema FetchSchema (string apiKey)
        {
            var url = "http://api.steampowered.com/IEconItems_440/GetSchema/v0001/?key=" + apiKey;

            string cachefile="tf_schema.cache";
            string result;

            HttpWebResponse response = SteamWeb.Request(url, "GET");

            DateTime SchemaLastModified = DateTime.Parse(response.Headers["Last-Modified"]);
           
            if (!System.IO.File.Exists(cachefile) || (SchemaLastModified> System.IO.File.GetCreationTime(cachefile)))
            {
                StreamReader reader = new StreamReader (response.GetResponseStream ());
                result = reader.ReadToEnd();
                File.WriteAllText(cachefile, result);
                System.IO.File.SetCreationTime(cachefile,SchemaLastModified);
            }
            else
            {
                TextReader reader = new StreamReader(cachefile);
                result = reader.ReadToEnd();
                reader.Close();
            }
            response.Close();

            SchemaResult schemaResult = JsonConvert.DeserializeObject<SchemaResult> (result);
            return schemaResult.result ?? null;
        }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("items_game_url")]
        public string ItemsGameUrl { get; set; }

        [JsonProperty("items")]
        public Item[] Items { get; set; }

        [JsonProperty("originNames")]
        public ItemOrigin[] OriginNames { get; set; }

        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Item GetItem (int defindex)
        {
            foreach (Item item in Items)
            {
                if (item.Defindex == defindex)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Returns all Items of the given crafting material.
        /// </summary>
        /// <param name="material">Item's craft_material_type JSON property.</param>
        /// <seealso cref="Item"/>
        public List<Item> GetItemsByCraftingMaterial(string material)
        {
            return Items.Where(item => item.CraftMaterialType == material).ToList();
        }

        public class ItemOrigin
        {
            [JsonProperty("origin")]
            public int Origin { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("item_class")]
            public string ItemClass { get; set; }

            [JsonProperty("item_type_name")]
            public string ItemTypeName { get; set; }

            [JsonProperty("item_name")]
            public string ItemName { get; set; }

            [JsonProperty("craft_material_type")]
            public string CraftMaterialType { get; set; }

            [JsonProperty("used_by_classes")]
            public string[] UsableByClasses { get; set; }

            [JsonProperty("item_slot")]
            public string ItemSlot { get; set; }

            [JsonProperty("craft_class")]
            public string CraftClass { get; set; }

            [JsonProperty("item_quality")]
            public int ItemQuality { get; set; }
        }

        protected class SchemaResult
        {
            public Schema result { get; set; }
        }

    }
}

