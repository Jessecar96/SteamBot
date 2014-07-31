using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SteamKit2;
using SteamTrade.TradeWebAPI;

namespace SteamTrade
{
    /// <summary>
    /// Steam (Generic) Inventory
    /// </summary>
    public partial class GenericInventory
    {
        public List<string> Errors { get; set; }
        public bool IsLoaded = false;

        #region JSON Stuff
        [JsonProperty("success")]
        private bool Success { get; set; }

        [JsonProperty("rgInventory")]
        public Dictionary<string, Item> Items { get; set; }

        [JsonProperty("rgDescriptions")]
        public Dictionary<string, ItemDescription> Descriptions { get; set; }

        /// <summary>
        /// ???
        /// </summary>
        public class Description
        {
            public string Type { get; set; }
            public string Value { get; set; }
            [JsonProperty("app_data")]
            public Dictionary<string, string> AppData { get; set; }
        }

        public class Attribute
        {
            [JsonProperty("internal_name")]
            public string InternalName { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            [JsonProperty("category_name")]
            public string CategoryName { get; set; }

            public override string ToString()
            {
                return string.Format("internal_name: {0}, name: {1}, category: {2}, category_name: {3}",
                    InternalName, Name, Category, CategoryName);
            }
        }

        public class Item : UserAsset
        {
            public string ClassId;
            public string InstanceId;
            public string DescriptionId { get { return ClassId + "_" + InstanceId; } }

            public override string ToString()
            {
                return string.Format("id:{0}, AppId:{1}, ContextId:{2}, Amount:{3}, classid:{4}, instanceid:{5}",
                    Id, AppId, ContextId, Amount, ClassId, InstanceId);
            }
        }

        public partial class ItemDescription
        {
            public string Name { get; set; }
            public string Type { get; set; }
            [JsonProperty("Tradable")]
            public bool IsTradable { get; set; }
            [JsonProperty("Marketable")]
            public bool IsMarketable { get; set; }
            public List<Description> Descriptions { get; set; }
            [JsonProperty("market_hash_name")]
            public string MarketHashName;

            [JsonProperty("tags")]
            public List<Attribute> Attributes { get; set; }

            public override string ToString()
            {
                return string.Format("name:{2}, type:{3}, tradable:{4}, marketable:{5}, total descriptions: {6}, total attributes:{7}",
                    Name, Type, IsTradable ? "yes" : "no", IsMarketable ? "yes" : "no", Descriptions.Count, Attributes.Count);
            }

            /// <summary>
            /// Output to console information from all descriptions.
            /// </summary>
            public void DebugDescriptions()
            {
                if (Descriptions == null)
                    return;

                Console.ForegroundColor = ConsoleColor.Gray;

                foreach (Description description in Descriptions)
                {
                    Console.WriteLine("TYPE: \"{0}\"\tVALUE: \"{1}\"", description.Type, description.Value);

                    if (description.AppData != null)
                    {
                        foreach (var data in description.AppData)
                        {
                            Console.WriteLine("APP_DATA >> KEY: \"{0}\"\t VALUE: \"{1}\"", data.Key, data.Value);
                        }
                    }

                    Console.Write("\n");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

            /// <summary>
            /// Output to console information from all attributes
            /// </summary>
            public void DebugAttributes()
            {
                if (Attributes == null)
                    return;

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("======== ATTRIBUTES ========");
                foreach (Attribute attribute in Attributes)
                {
                    Console.WriteLine("CATEGORY\tINTERNAL_NAME\n{0}\t{1}\nCATEGORY_NAME\tNAME\n{2}\t{3}\n",
                        attribute.Category, attribute.InternalName, attribute.CategoryName, attribute.Name);
                }
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        #endregion

        public GenericInventory()
        {
            Errors = new List<string>();
            Items = new Dictionary<string, Item>();
            Descriptions = new Dictionary<string, ItemDescription>();
        }

        public static void LogError(string message)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.Write("[GenericInventory Error]");
            Console.BackgroundColor = ConsoleColor.Black;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" " + message);
            Console.ForegroundColor = ConsoleColor.White;

            try
            {
                System.IO.File.AppendAllText(@"logs\GenericInventory.txt", message + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.Write("[ERROR]");
                Console.BackgroundColor = ConsoleColor.Black;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" " + e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        public static GenericInventory Load(SteamID steamId, int appId, IEnumerable<int> contextId)
        {
            GenericInventory Combined = new GenericInventory();
            GenericInventory tmp;

            foreach (int contextid in contextId)
            {
                tmp = Load(steamId, appId, contextid);

                foreach (var item in tmp.Items)
                {
                    if (Combined.Items.Contains(item))
                        continue;

                    Combined.Items.Add(item.Key, item.Value);
                }
                foreach (var description in tmp.Descriptions)
                {
                    if (Combined.Descriptions.Contains(description))
                        continue;

                    Combined.Descriptions.Add(description.Key, description.Value);
                }
            }
            return Combined;
        }

        public static GenericInventory Load(SteamID steamId, int appId, int contextId = 2)
        {
            try
            {
                string response = SteamWeb.Fetch(string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/",
                    steamId.ConvertToUInt64(), appId, contextId), "GET", null, null, true);

                return Deserialize(response, appId, contextId);
            }
            catch (Exception e)
            {
                LogError(string.Format("Load(SteamId:{0}, AppId:{1}, ContextId:{2}) >> {3}",
                    steamId.ConvertToUInt64(), appId, contextId, e.Message));

                return new GenericInventory();
            }
        }

        public static GenericInventory Deserialize(string rawJson, int AppId, int ContextId)
        {
            try
            {
                GenericInventory jsonResponse = JsonConvert.DeserializeObject<GenericInventory>(rawJson);

                if (!jsonResponse.Success || jsonResponse.Items == null)
                    return new GenericInventory();

                //Set UserAsset properties so it can be used with Trade.AddItem(UserAsset item)
                foreach (Item item in jsonResponse.Items.Values)
                {
                    item.AppId = AppId;
                    item.ContextId = ContextId;
                }

                jsonResponse.IsLoaded = true;
                return jsonResponse;
            }
            catch (Exception e)
            {
                if (e.GetType() != typeof(Newtonsoft.Json.JsonSerializationException))
                {
                    LogError(string.Format("Deserialize(rawJson , AppId:{0}, ContextId:{1}) >> {2}",
                        AppId, ContextId, e.Message));
                }
                return new GenericInventory();
            }
        }

        /// <summary>
        /// Returns a list of item ids which have the same ClassId (Schema Definition).
        /// </summary>
        /// <param name="classId">Schema Id</param>
        /// <returns></returns>
        public List<ulong> GetItemIdsByClassId(string classId)
        {
            List<ulong> list = new List<ulong>();

            foreach (Item item in Items.Values)
            {
                if (item.ClassId == classId)
                    list.Add(item.Id);
            }

            return list;
        }

        /// <summary>
        /// Returns a list of item ids which have the same DescriptionId (exactly the same properties).
        /// </summary>
        /// <param name="descriptionId">Item Properties Id</param>
        /// <returns></returns>
        public List<ulong> GetItemIdsByDescriptionId(string descriptionId)
        {
            List<ulong> list = new List<ulong>();

            foreach (Item item in Items.Values)
            {
                if (item.DescriptionId == descriptionId)
                    list.Add(item.Id);
            }

            return list;
        }

        public GenericInventory.Item GetItem(ulong id)
        {
            return GetItem(id.ToString());
        }
        public GenericInventory.Item GetItem(string id)
        {
            GenericInventory.Item item;

            if (Items.TryGetValue(id, out item))
                return item;
            else
                return null;
        }

        public GenericInventory.ItemDescription GetDescription(UserAsset item)
        {
            return GetDescription(GetItem(item.Id));
        }
        public GenericInventory.ItemDescription GetDescription(ulong id)
        {
            return GetDescription(GetItem(id));
        }
        public GenericInventory.ItemDescription GetDescription(Item item)
        {
            if (item == null)
                return null;

            ItemDescription description;

            if (Descriptions.TryGetValue(item.DescriptionId, out description))
                return description;
            else
                return null;
        }
    }
}
