using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SteamTrade
{
    /// <summary>
    /// This class represents the TF2 Item schema as deserialized from its
    /// JSON representation.
    /// </summary>
    public class Schema
    {
        private const string SchemaMutexName = "steam_bot_cache_file_mutex";
        private const string SchemaApiUrlBase = "http://api.steampowered.com/IEconItems_440/GetSchema/v0001/?key=";
        private const string cachefile = "tf_schema.cache";

        /// <summary>
        /// Fetches the Tf2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>
        public static async Task<Schema> FetchSchema (string apiKey)
        {   
            var url = SchemaApiUrlBase + apiKey;

            // just let one thread/proc do the initial check/possible update.
            bool wasCreated;
            var mre = new EventWaitHandle(false, 
                EventResetMode.ManualReset, SchemaMutexName, out wasCreated);

            // the thread that create the wait handle will be the one to 
            // write the cache file. The others will wait patiently.
            if (!wasCreated)
            {
                bool signaled = mre.WaitOne(10000);

                if (!signaled)
                {
                    return null;
                }
            }

            using(HttpWebResponse response = await new SteamWeb().Request(url, "GET"))
            {
                DateTime schemaLastModified = response.LastModified;

                string result = await GetSchemaString(response, schemaLastModified);

                // were done here. let others read.
                mre.Set();

                SchemaResult schemaResult = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<SchemaResult>(result));
                return schemaResult.result ?? null;
            }
        }

        // Gets the schema from the web or from the cached file.
        private static async Task<string> GetSchemaString(HttpWebResponse response, DateTime schemaLastModified)
        {
            string result;
            bool mustUpdateCache = !File.Exists(cachefile) || schemaLastModified > File.GetCreationTime(cachefile);

            if (mustUpdateCache)
            {
                using(var reader = new StreamReader(response.GetResponseStream()))
                {
                    result = await reader.ReadToEndAsync();

                    File.WriteAllText(cachefile, result);
                    File.SetCreationTime(cachefile, schemaLastModified);
                }
            }
            else
            {
                // read previously cached file.
                using(TextReader reader = new StreamReader(cachefile))
                {
                    result = await reader.ReadToEndAsync();
                }
            }

            return result;
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

        public List<Item> GetItems()
        {
            return Items.ToList();
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

        protected class SchemaResult : SteamWeb.ResponseBase
        {
            public Schema result { get; set; }
        }

    }
}

