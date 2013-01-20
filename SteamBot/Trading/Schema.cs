using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace SteamBot.Trading
{
    public class Schema
    {

        public SteamSchema itemSchema;

        public Schema(int appId, string apiKey)
        {
            string url = String.Format("/IEconItems_{0}/GetSchema/v0001/?key={1}",
                appId, apiKey);
            string cacheFile = String.Format("{0}.cacheFile", appId);
            Web web = new Web
            {
                Scheme = "http",
                Domain = "api.steampowered.com"
            };
            HttpWebRequest request = web.GetRequest(url, "GET", null);
            DateTime lastUpdated = File.GetCreationTimeUtc(cacheFile);
            DateTime schemaModifiedOn;
            HttpWebResponse response;
            string result;

            request.IfModifiedSince = lastUpdated;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                schemaModifiedOn = response.LastModified.ToUniversalTime();
            }
            catch (WebException e)
            {
                HttpWebResponse r = e.Response as HttpWebResponse;
                if (r.StatusCode == HttpStatusCode.NotModified)
                {
                    response = r;
                    schemaModifiedOn = lastUpdated.Subtract(new TimeSpan(10000000000));
                }
                else
                {
                    throw e;
                }
            }

            if (schemaModifiedOn > lastUpdated)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                result = reader.ReadToEnd();
                File.WriteAllText(cacheFile, result, Encoding.UTF8);
                File.SetCreationTimeUtc(cacheFile, DateTime.UtcNow);
            }
            else
            {
                TextReader reader = new StreamReader(cacheFile, Encoding.UTF8);
                result = reader.ReadToEnd();
                reader.Close();
            }

            itemSchema = JsonConvert.DeserializeObject<Result>(result).result;
            
        }

        #region JSON Responses
        public class Result
        {
            [JsonProperty("result")]
            public SteamSchema result { get; set; }
        }

        public class SteamSchema
        {
            [JsonProperty("status")]
            public int Status { get; set; }

            [JsonProperty("items_game_url")]
            public string ItemsGameUrl { get; set; }

            [JsonProperty("items")]
            public Item[] Items { get; set; }

            [JsonProperty("originNames")]
            public ItemOrigin[] OriginNames { get; set; }
        }

        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("item_class")]
            public string Class { get; set; }

            [JsonProperty("item_type_name")]
            public string TypeName { get; set; }

            [JsonProperty("item_name")]
            public string ItemName { get; set; }

            [JsonProperty("craft_material_type")]
            public string CraftMaterialType { get; set; }

            [JsonProperty("used_by_classes")]
            public string[] UsableByClasses { get; set; }

            [JsonProperty("item_slot")]
            public string Slot { get; set; }

            [JsonProperty("craft_class")]
            public string CraftClass { get; set; }

            [JsonProperty("item_quality")]
            public int Quality { get; set; }
        }

        public class ItemOrigin
        {
            [JsonProperty("origin")]
            public int Origin { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
        #endregion
    }
}
