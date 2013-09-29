using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;
using System.Text;

namespace SteamTrade
{
    /// <summary>
    /// This class represents the TF2 Item schema as deserialized from its
    /// JSON representation.
    /// </summary>
    public class Test
    {
        private const string SchemaMutexName = "dota2items.json";
        private const string cachefile = "dota2items.json";
        private const string cachefileinfo = "dota2info.txt";
        /// <summary>
        /// Fetches the Tf2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>
        public static Test FetchSchema(string url, bool force=false)
        {
            bool wasCreated;
            var mre = new EventWaitHandle(false, 
                EventResetMode.ManualReset, SchemaMutexName, out wasCreated);

            // the thread that create the wait handle will be the one to 
            // write the cache file. The others will wait patiently.
            if (!wasCreated)
            {
                bool signaled = false;

                while (!signaled)
                {
                    signaled = mre.WaitOne(10000);
                }   
            }
            bool needhttp = true;
            SchemaResult schemaResult=null;
            if (File.Exists (cachefile)&&File.Exists (cachefileinfo ))
            {
                Console.Write("info及json存在");
                string cache;
                TextReader readercache = new StreamReader(cachefileinfo );
                cache = readercache.ReadToEnd();
                readercache.Close();
                if (cache == url)
                {
                    Console.Write("url未变化");
                //    try
                //    {

                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                        request.Method = "HEAD";
                        request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                        request.Host = "media.steampowered.com";
                        request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
                        request.Referer = "http://media.steampowered.com";
                        request.IfModifiedSince = File.GetCreationTime(cachefile);
                        HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            needhttp = false;
                            Console.Write("文件未修改,无需下载");
                        }
                        else
                        {
                            needhttp = true;
                            Console.Write("文件已修改,重新下载");
                        }
                 //   }
                   /* catch
                    {
                        Console.WriteLine("http状态获取失败");
                        needhttp = true;
                    } */
                }
                else
                {
                    Console.WriteLine(cache.Length);
                    Console.WriteLine(url.Length);
                    Console.WriteLine();
                    Console.Write(cache);
                    Console.WriteLine();
                    Console.Write(url);
                    Console.WriteLine();
                    Console.Write("url已变化");
                    Console.WriteLine();
                }
            }
            if (!needhttp)
            {
                Console.Write("需要重新下载");
                try
                {
                    StreamReader reader = new StreamReader(cachefile);
                    Stream urlresult = reader.BaseStream ;
                    //string result = GetSchemaString();
                    // TextWriter aaa = new StreamWriter(cachefile ,true);
                    //aaa.Write(urlresult);
                    Convertvdf2json(urlresult, cachefile, false);
                    string result = GetSchemaString();
                    schemaResult = JsonConvert.DeserializeObject<SchemaResult>(result);
                    //return schemaResult.items_game ?? null;
                }
                catch
                {
                    needhttp = true;
                }
            }
            while (needhttp) //假如已保存的文件无效则连接http获取新的文件.
            {
                try
                {
                    needhttp = false;
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                    request.Method = "GET";
                    request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                    request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    request.Host = "media.steampowered.com";
                    request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
                    request.Referer = "http://media.steampowered.com";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    DateTime schemaLastModified = DateTime.Parse(response.Headers["Last-Modified"]);
                    Stream urlresult = response.GetResponseStream();
                    Convertvdf2json(urlresult, cachefile, false);
                    File.SetCreationTime(cachefile, schemaLastModified);
                    StreamWriter wf = new StreamWriter(cachefileinfo , false);
                    wf.Write(url);
                    wf.Close();
                    response.Close();
                    request.Abort();
                    string result = GetSchemaString();
                    schemaResult = JsonConvert.DeserializeObject<SchemaResult>(result);
                   // return schemaResult.items_game ?? null;

                }
                catch
                {
                    needhttp = true;
                }
 
            }
            return schemaResult.items_game ?? null; 
            
        }
        public static void Convertvdf2json(Stream inputstream, string outputpath, bool CompactJSON)
        {

            using (FileStream stream2 = System.IO.File.Create(outputpath))
            {
                KeyValue kv = new KeyValue(null, null);

                kv.ReadAsText(inputstream);
                Convert(kv, stream2, CompactJSON);
            }

        }
        public static void Convert(KeyValue kv, Stream outputStream, bool compactJSON)
        {
            JObject obj2 = new JObject();
            new JObject();
            obj2[kv.Name] = ConvertRecursive(kv);
            using (StreamWriter writer = new StreamWriter(outputStream, Encoding.UTF8))
            {
                using (JsonTextWriter writer2 = new JsonTextWriter(writer))
                {
                    writer2.Formatting = compactJSON ? Formatting.None : Formatting.Indented;
                    obj2.WriteTo(writer2, new JsonConverter[0]);
                }
            }
        }

        private static JToken ConvertRecursive(KeyValue kv)
        {
            JObject obj2 = new JObject();
            if (kv.Children.Count <= 0)
            {
                return kv.Value;
            }
            foreach (KeyValue value2 in kv.Children)
            {
                obj2[value2.Name] = ConvertRecursive(value2);
            }
            return obj2;
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
        public Dictionary <string ,Item > Items{ get; set; }

        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Item GetItem (int defindex)
        {

            string x = defindex.ToString();
            if (Items.ContainsKey(x))
            {
                return Items[x];
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Returns all Items of the given crafting material.
        /// </summary>
        /// <param name="material">Item's craft_material_type JSON property.</param>
        /// <seealso cref="Item"/>
        
        /*
        public List<Item> GetItems()
        {
            //return Items.ToList();
        } */

        

        public class Item
        {
           // [JsonProperty("defindex")]
           // public ushort Defindex { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("item_rarity")]
            public string Item_rarity { get; set; }

            [JsonProperty("prefab")]
            public string Prefab { get; set; }

            [JsonProperty("item_set")]
            public string Item_set { get; set; }

            [JsonProperty("model_player")]
            public string Model_player { get; set; }


            
        }

        protected class SchemaResult
        {
            public Test items_game { get; set; }
        }

    }
}

