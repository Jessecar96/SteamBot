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
    public class UserItem
    {

        private const string cachefile = "useritem.json";

        /// <summary>
        /// Fetches the Tf2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>
        public static UserItem FetchSchema()
        {
            
            string result = GetSchemaString();
            UserItemResult schemaResult = JsonConvert.DeserializeObject<UserItemResult>(result);
            return schemaResult.result ?? null;
           // return schemaResult.result;
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

        

        [JsonProperty("useritems")]
        //public Useritem[] Items { get; set; }
        public List <Useritem> Items { get; set; }
        

        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Useritem GetItem(int defindex)
        {
            // if (defindex < Items.Length)
          //  {
             //   return Items[defindex];
           // }

            foreach (Useritem item in Items)
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
        

        public List<Useritem> GetItems()
        {
            return Items.ToList();
        }

        

        public class Useritem
        {
            [JsonProperty("steam64id")]
            public UInt64  Steam64id { get; set; }
            

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

            [JsonProperty("pricekey")]
            public int Pricekey { get; set; }

            [JsonProperty("pricerr")]
            public int Pricerr { get; set; }
            
        }

        protected class UserItemResult
        {
            public UserItem result { get; set; }
        }

    }
}

