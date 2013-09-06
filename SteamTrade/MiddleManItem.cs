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
    public class MiddleManItem
    {

        private const string cachefile = "middlemanitem.json";

        /// <summary>
        /// Fetches the Tf2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>
        public static MiddleManItem FetchSchema()
        {
            
            string result = GetSchemaString();
          //  UserItemResult schemaResult = JsonConvert.DeserializeObject<UserItemResult>(result);
          //  return schemaResult.result  ?? null;
            MiddleManItem schemaResult = JsonConvert.DeserializeObject<MiddleManItem>(result);
            return schemaResult ?? null;
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

        

        [JsonProperty("records")]
        //public Useritem[] Items { get; set; }
        public List<Record> Records { get; set; }

        [JsonProperty("alipayaccounts")]
        public List<Alipayaccount> Alipayaccounts { get; set; }

        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Record GetItem(int defindex)
        {
            // if (defindex < Items.Length)
          //  {
             //   return Items[defindex];
           // }

            foreach (Record item in Records)
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


        public List<Record> GetItems()
        {
            return Records.ToList();
        }

        

        public class Record
        {
            [JsonProperty("recordid")]
            public string Recordid { get; set; }

            [JsonProperty("sellersteam64id")]
            public UInt64 Sellersteam64id { get; set; }

            [JsonProperty("buyersteam64id")]
            public UInt64 Buyersteam64id { get; set; }

            [JsonProperty("id")]
            public ulong Id { get; set; }
                        
            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("item_name")]
            public string  Item_name { get; set; }

            [JsonProperty("error")]
            public bool Error { get; set; }

            [JsonProperty("status")]
            public int Status { get; set; }

            [JsonProperty("sellercredititems")]
            public List<ulong> Sellercredititems { get; set; }

            [JsonProperty("buyercredititems")]
            public List<ulong> Buyercredititems { get; set; }

            [JsonProperty("selleritemsstatus")]
            public int Selleritemsstatus { get; set; }

            [JsonProperty("buyeritemsstatus")]
            public int Buyeritemsstatus { get; set; }
            
        }

        public class Alipayaccount
        {
            [JsonProperty("steam64id")]
            public UInt64 Steam64id { get; set; }

            [JsonProperty("account")]
            public string Account { get; set; }

            [JsonProperty("settime")]
            public string Settime { get; set; }
        }
        /*
        protected class UserItemResult
        {
            public UserItem result { get; set; }
        }
         * */

    }
}

