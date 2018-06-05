using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Threading;

namespace SteamTrade
{
    /// <summary>
    /// This class represents the TF2 Item schema as deserialized from its
    /// JSON representation.
    /// </summary>
    public class Schema
    {
        private const string SchemaMutexName = "steam_bot_cache_file_mutex";
        private const string SchemaApiUrlBase = "https://api.steampowered.com/IEconItems_440/GetSchemaItems/v1/?key=";
        private const string SchemaApiItemOriginNamesUrlBase = "https://api.steampowered.com/IEconItems_440/GetSchemaOverview/v1/?key=";

        /// <summary>
        /// Full file name for schema cache file. This value is only used when calling <see cref="FetchSchema"/>. If the time modified of the local copy is later than that of the server, local copy is used without downloading. Default value is %TEMP%\tf_schema.cache.
        /// </summary>
        public static string CacheFileFullName = Path.GetTempPath() + "\\tf_schema.cache";

        /// <summary>
        /// Fetches the Tf2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>

        public static Schema FetchSchema(string apiKey, string schemaLang = null)
        {
            var url = SchemaApiUrlBase + apiKey;
            if (schemaLang != null)
                url += "&format=json&language=" + schemaLang;

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

            bool keepUpdating = true;
            SchemaResult schemaResult = new SchemaResult();
            string tmpUrl = url;

            do
            {
                if(schemaResult.result != null)
                    tmpUrl = url + "&start=" + schemaResult.result.Next;

                string result = new SteamWeb().Fetch(tmpUrl, "GET");

                if (schemaResult.result == null || schemaResult.result.Items == null)
                {
                    schemaResult = JsonConvert.DeserializeObject<SchemaResult>(result);
                }
                else
                {
                    SchemaResult tempResult = JsonConvert.DeserializeObject<SchemaResult>(result);
                    var items = schemaResult.result.Items.Concat(tempResult.result.Items);
                    schemaResult.result.Items = items.ToArray();
                    schemaResult.result.Next = tempResult.result.Next;
                }

                if (schemaResult.result.Next <= schemaResult.result.Items.Count())
                    keepUpdating = false;

            } while (keepUpdating);


            //Get origin names
            string itemOriginUrl = SchemaApiItemOriginNamesUrlBase + apiKey;

            if (schemaLang != null)
                itemOriginUrl += "&format=json&language=" + schemaLang;

            string resp = new SteamWeb().Fetch(itemOriginUrl, "GET");

            var itemOriginResult = JsonConvert.DeserializeObject<SchemaResult>(resp);

            schemaResult.result.OriginNames = itemOriginResult.result.OriginNames;

            // were done here. let others read.
            mre.Set();
            DateTime schemaLastModified = DateTime.Now;

            return schemaResult.result ?? null;
            
        }       
                       
        // Gets the schema from the web or from the cached file.
        private static string GetSchemaString(HttpWebResponse response, DateTime schemaLastModified)
        {
            string result;
            bool mustUpdateCache = !File.Exists(CacheFileFullName) || schemaLastModified > File.GetCreationTime(CacheFileFullName);

            if (mustUpdateCache)
            {
                using(var reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();

                    File.WriteAllText(CacheFileFullName, result);
                    File.SetCreationTime(CacheFileFullName, schemaLastModified);
                }
            }
            else
            {
                // read previously cached file.
                using(TextReader reader = new StreamReader(CacheFileFullName))
                {
                    result = reader.ReadToEnd();
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

        [JsonProperty("next")]
        public int Next { get; set; }

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
            
            [JsonProperty("proper_name")]
            public bool ProperName { get; set; }

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

