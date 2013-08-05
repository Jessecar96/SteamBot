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
    public class Dota2
    {

        private const string cachefile = "dota2items.cache";

        /// <summary>
        /// Fetches the Tf2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>
        public static Dota2 FetchSchema ()
        {
            
            string result = GetSchemaString();
            SchemaResult schemaResult = JsonConvert.DeserializeObject<SchemaResult>(result);
            //return schemaResult.result ?? null;
            return schemaResult.result;
            /*
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
            }  */
        }

        // Gets the schema from the web or from the cached file.
        private static string GetSchemaString()
        {
            string result;
            
                TextReader reader = new StreamReader(cachefile);
                result = reader.ReadToEnd();
                reader.Close();
          
            return result;
        }

        

        [JsonProperty("items")]
        public Item[] Items { get; set; }

        

        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Item GetItem (int defindex)
        {
            // if (defindex < Items.Length)
          //  {
             //   return Items[defindex];
           // }
           
            foreach (Item item in Items)
            {
                if (item.Defindex == defindex)
                    return item;
            } 
           // else
           // {
           return null;
           // }
        }

        /// <summary>
        /// Returns all Items of the given crafting material.
        /// </summary>
        /// <param name="material">Item's craft_material_type JSON property.</param>
        /// <seealso cref="Item"/>
        

        public List<Item> GetItems()
        {
            return Items.ToList();
        }

        

        public class Item
        {
            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("item_rarity")]
            public string Item_rarity { get; set; }

            [JsonProperty("prefab")]
            public string Prefab { get; set; }

            
        }

        protected class SchemaResult
        {
            public Dota2 result { get; set; }
        }

    }
}

