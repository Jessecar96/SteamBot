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
    public class Dota2Schema
    {
        public static Dota2Schema Schema;

        private const string SchemaMutexName = "steam_bot_cache_file_mutex";
        private const string SchemaApiUrlBase = "http://api.steampowered.com/IEconItems_570/GetSchema/v0001/?key=";
        private const string cachefile = "schema_dota2.cache";

        /// <summary>
        /// Fetches the Dota 2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>
        public static Dota2Schema FetchSchema (string apiKey)
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

            HttpWebResponse response = SteamWeb.Request(url, "GET");
            DateTime schemaLastModified = response.LastModified;
            string result = GetSchemaString(response, schemaLastModified);
            response.Close();
            mre.Set();

            SchemaResult schemaResult = JsonConvert.DeserializeObject<SchemaResult> (result);
            return schemaResult.result ?? null;
        }

        // Gets the schema from the web or from the cached file.
        private static string GetSchemaString(HttpWebResponse response, DateTime schemaLastModified)
        {
            string result;
            bool mustUpdateCache = !File.Exists(cachefile) || schemaLastModified > File.GetCreationTime(cachefile);

            if (mustUpdateCache)
            {
                var reader = new StreamReader(response.GetResponseStream());
                result = reader.ReadToEnd();

                File.WriteAllText(cachefile, result);
                File.SetCreationTime(cachefile, schemaLastModified);
            }
            else
            {
                // read previously cached file.
                TextReader reader = new StreamReader(cachefile);
                result = reader.ReadToEnd();
                reader.Close();
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

            [JsonProperty("item_description")]
            public string ItemDescription { get; set; }

            [JsonProperty("proper_name")]
            public bool IsProperName { get; set; }

            [JsonProperty("item_quality")]
            public int ItemQuality { get; set; }

            [JsonProperty("item_set")]
            private string itemSet { get; set; }
            public string ItemSet
            {
                get
                {
                    return string.IsNullOrEmpty(itemSet) ? "" : itemSet;
                }
            }

            [JsonProperty("capabilities")]
            public Capabilities Capabilities { get; set; }
        }

        public class Capabilities
        {
            [JsonProperty("nameable")]
            private bool isNameable { get; set; }
            public bool IsNameable
            {
                get
                {
                    try
                    {
                        return isNameable;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            [JsonProperty("can_craft_mark")]
            public bool IsCraftable { get; set; }

            [JsonProperty("can_be_restored")]
            public bool IsRestorable { get; set; }

            [JsonProperty("strange_parts")]
            public bool HasStrangeParts { get; set; }

            [JsonProperty("paintable_unusual")]
            public bool IsPaintableUnusual { get; set; }

            [JsonProperty("autograph")]
            public bool IsAutographable { get; set; }

            [JsonProperty("can_consume")]
            public bool IsConsumable { get; set; }

            [JsonProperty("can_have_sockets")]
            private bool isSocketable { get; set; }
            public bool IsSocketable
            {
                get
                {
                    try
                    {
                        return isSocketable;
                    }
                    catch
                    {
                        return false;
                    }
                }                
            }            
        }

        protected class SchemaResult
        {
            public Dota2Schema result { get; set; }
        }
    }
}

