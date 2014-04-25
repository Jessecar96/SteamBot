using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;
using SteamTrade.TradeWebAPI;
using System.Text.RegularExpressions;
using System.Net;

namespace SteamTrade
{
    /// <summary>
    /// Generic Steam Backpack Interface
    /// </summary>
    public class GenericInventory
    {
        private Dictionary<int, Dictionary<long, Inventory>> inventories = new Dictionary<int, Dictionary<long, Inventory>>();
        private Dictionary<int, Dictionary<long, Task>> InventoryTasks = new Dictionary<int, Dictionary<long, Task>>();
        private Task ConstructTask = null;
        private static CookieContainer Cookies = null;
        private const int WEB_REQUEST_MAX_RETRIES = 3;
        private const int WEB_REQUEST_TIME_BETWEEN_RETRIES_MS = 1000;

        /// <summary>
        /// Gets the content of all inventories listed in http://steamcommunity.com/profiles/STEAM_ID/inventory/
        /// </summary>
        public Dictionary<int, Dictionary<long, Inventory>> Inventories
        {
            get
            {
                WaitAllTasks();
                return inventories;
            }
        }

        public GenericInventory(SteamID steamId)
        {
            ConstructTask = Task.Factory.StartNew(() =>
            {
                string inventoryUrl = "http://steamcommunity.com/profiles/" + steamId.ConvertToUInt64() + "/inventory/";
                string response = RetryWebRequest(inventoryUrl);
                Regex reg = new Regex("var g_rgAppContextData = (.*?);");                
                Match m = reg.Match(response);
                if (m.Success)
                {
                    string json = m.Groups[1].Value;
                    try
                    {
                        var schemaResult = JsonConvert.DeserializeObject<Dictionary<int, InventoryApps>>(json);
                        foreach (var app in schemaResult)
                        {
                            int appId = app.Key;
                            InventoryTasks[appId] = new Dictionary<long, Task>();
                            foreach (var context in app.Value.RgContexts)
                            {
                                long contextId = context.Key;
                                InventoryTasks[appId][contextId] = Task.Factory.StartNew(() =>
                                {
                                    var inventory = FetchInventory(appId, contextId, steamId);
                                    if (!inventories.ContainsKey(appId))
                                        inventories[appId] = new Dictionary<long, Inventory>();
                                    if (inventory != null)
                                        inventories[appId].Add(contextId, inventory);
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Success = false;
                        Console.WriteLine(ex);
                    }
                }
                else
                {
                    Success = false;
                    IsPrivate = true;
                }
            });       
        }

        public static void SetCookie(CookieContainer cookies)
        {
            Cookies = cookies;
        }

        /// <summary>
        /// Use this to iterate through items in the inventory.
        /// </summary>
        /// <param name="appId">App ID</param>
        /// <param name="contextId">Context ID</param>
        /// <returns>A List containing GenericInventory.Inventory.Item elements</returns>
        public List<GenericInventory.Inventory.Item> GetInventory(int appId, int contextId)
        {
            return Inventories[appId][contextId].RgDescriptions.Values.ToList();
        }

        private Inventory FetchInventory(int appId, long contextId, SteamID steamId)
        {
            string inventoryUrl = string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/", steamId.ConvertToUInt64(), appId, contextId);
            string response = RetryWebRequest(inventoryUrl);
            try
            {
                var inventory = JsonConvert.DeserializeObject<Inventory>(response);
                foreach (var dictItem in inventory.RgInventory)
                {
                    var item = dictItem.Value;
                    var classId = item.ClassId;
                    var instanceId = item.InstanceId;
                    var key = string.Format("{0}_{1}", classId, instanceId);
                    var inventoryItem = inventory.RgDescriptions[key];
                    inventoryItem.ContextId = contextId;
                    inventoryItem.Id = item.Id;                    
                    inventoryItem.Amount = item.Amount;
                    inventoryItem.IsCurrency = false;
                    inventoryItem.Position = item.Position;
                }
                foreach (var dictItem in inventory.RgCurrencies)
                {
                    var item = dictItem.Value;
                    var classId = item.ClassId;
                    var instanceId = 0;
                    var key = string.Format("{0}_{1}", classId, instanceId);
                    var inventoryItem = inventory.RgDescriptions[key];
                    inventoryItem.ContextId = contextId;
                    inventoryItem.Id = item.Id;
                    inventoryItem.Amount = item.Amount;
                    inventoryItem.IsCurrency = item.IsCurrency;
                    inventoryItem.Position = item.Position;
                }
                return inventory;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to deserialize {0}:{1}.", appId, contextId);
                Console.WriteLine(ex);
                return null;
            }
        }

        public Inventory.Item GetItem(int appId, long contextId, ulong id)
        {
            try
            {
                ConstructTask.Wait();
                InventoryTasks[appId][contextId].Wait();
                var inventory = inventories[appId];
                Inventory.ItemInfo item = null;
                if (inventory[contextId].RgInventory.ContainsKey(id.ToString()))
                    item = inventory[contextId].RgInventory[id.ToString()];
                Inventory.CurrencyItem currencyItem = null;
                if (inventory[contextId].RgCurrencies.ContainsKey(id.ToString()))
                    currencyItem = inventory[contextId].RgCurrencies[id.ToString()];
                var key = string.Format("{0}_{1}", item == null ? currencyItem.ClassId : item.ClassId, item == null ? 0 : item.InstanceId);
                return inventory[contextId].RgDescriptions[key];
            }
            catch
            {
                return null;
            }            
        }

        private void WaitAllTasks()
        {
            ConstructTask.Wait();
            foreach (var task in InventoryTasks)
            {
                foreach (var contextTask in task.Value)
                {
                    contextTask.Value.Wait();
                }
            }
        }

        /// <summary>
        /// Calls the given function multiple times, until we get a non-null/non-false/non-zero result, or we've made at least
        /// WEB_REQUEST_MAX_RETRIES attempts (with WEB_REQUEST_TIME_BETWEEN_RETRIES_MS between attempts)
        /// </summary>
        /// <returns>The result of the function if it succeeded, or an empty string otherwise</returns>
        private string RetryWebRequest(string url)
        {
            for (int i = 0; i < WEB_REQUEST_MAX_RETRIES; i++)
            {
                try
                {
                    return SteamWeb.Fetch(url, "GET", null, Cookies);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                if (i != WEB_REQUEST_MAX_RETRIES)
                {
                    System.Threading.Thread.Sleep(WEB_REQUEST_TIME_BETWEEN_RETRIES_MS);
                }
            }
            return "";
        }

        public bool Success { get { return true; } private set; }
        public bool IsPrivate { get { return false; } private set; }

        public class GenericItem
        {
            public int AppId { get; private set; }
            public long ContextId { get; private set; }
            public ulong ItemId { get; private set; }
            public int Amount { get; private set; }

            public GenericItem(int appId, long contextId, ulong itemId, int amount)
            {
                this.AppId = appId;
                this.ContextId = contextId;
                this.ItemId = itemId;
                this.Amount = amount;
            }
        }

        public class InventoryApps
        {
            [JsonProperty("appid")]
            public int AppId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("icon")]
            public string Icon { get; set; }

            [JsonProperty("link")]
            public string Link { get; set; }

            [JsonProperty("asset_count")]
            public int AssetCount { get; set; }

            [JsonProperty("inventory_logo")]
            public string InventoryLogo { get; set; }

            [JsonProperty("trade_permissions")]
            public string TradePermissions { get; set; }

            [JsonProperty("rgContexts")]
            public Dictionary<long, RgContext> RgContexts { get; set; }

            public class RgContext
            {
                [JsonProperty("asset_count")]
                public int AssetCount { get; set; }

                [JsonProperty("id")]
                public string Id { get; set; }

                [JsonProperty("name")]
                public string Name { get; set; }
            }
        }

        public class Inventory
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("rgInventory")]
            private dynamic rgInventory { get; set; }
            public Dictionary<string, ItemInfo> RgInventory
            {
                // for some games rgInventory will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, ItemInfo>>(Convert.ToString(rgInventory));
                        return dictionary;
                    }
                    catch
                    {
                        return new Dictionary<string, ItemInfo>();
                    }
                }
                set
                {
                    rgInventory = value;
                }
            }

            [JsonProperty("rgCurrency")]
            private dynamic rgCurrency { get; set; }
            public Dictionary<string, CurrencyItem> RgCurrencies
            {
                // for some games rgCurrency will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, CurrencyItem>>(Convert.ToString(rgCurrency));
                        return dictionary;
                    }
                    catch
                    {
                        return new Dictionary<string, CurrencyItem>();
                    }
                }
                set
                {
                    rgCurrency = value;
                }
            }

            [JsonProperty("rgDescriptions")]
            private dynamic rgDescriptions { get; set; }
            public Dictionary<string, Item> RgDescriptions
            {
                get
                {
                    try
                    {
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, Item>>(Convert.ToString(rgDescriptions));
                        return dictionary;
                    }
                    catch
                    {
                        return new Dictionary<string, Item>();
                    }
                }
                set
                {
                    rgDescriptions = value;
                }
            }

            [JsonProperty("more")]
            public bool More { get; set; }

            [JsonProperty("more_start")]
            public bool MoreStart { get; set; }

            public class ItemInfo
            {
                [JsonProperty("id")]
                public ulong Id { get; set; }

                [JsonProperty("classid")]
                public ulong ClassId { get; set; }

                [JsonProperty("instanceid")]
                public ulong InstanceId { get; set; }

                [JsonProperty("amount")]
                public int Amount { get; set; }

                [JsonProperty("pos")]
                public int Position { get; set; }
            }

            public class CurrencyItem
            {
                [JsonProperty("id")]
                public ulong Id { get; set; }

                [JsonProperty("classid")]
                public ulong ClassId { get; set; }

                [JsonProperty("amount")]
                public int Amount { get; set; }

                [JsonProperty("is_currency")]
                public bool IsCurrency { get; set; }

                [JsonProperty("pos")]
                public int Position { get; set; }
            }

            public class Item
            {
                public ulong Id { get; set; }
                public int Amount { get; set; }
                public bool IsCurrency { get; set; }
                public int Position { get; set; }
                public long ContextId { get; set; }

                [JsonProperty("appid")]
                public int AppId { get; set; }

                [JsonProperty("classid")]
                public ulong ClassId { get; set; }

                [JsonProperty("instanceid")]
                public ulong InstanceId { get; set; }

                [JsonProperty("icon_url")]
                public string IconUrl { get; set; }

                [JsonProperty("icon_url_large")]
                public string IconUrlLarge { get; set; }

                [JsonProperty("icon_drag_url")]
                public string IconDragUrl { get; set; }

                [JsonProperty("name")]
                public string Name { get; set; }

                [JsonProperty("market_hash_name")]
                public string MarketHashName { get; set; }

                [JsonProperty("market_name")]
                public string MarketName { get; set; }

                [JsonProperty("name_color")]
                public string NameColor { get; set; }

                [JsonProperty("background_color")]
                public string BackgroundColor { get; set; }

                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("tradable")]
                private short isTradable { get; set; }
                public bool IsTradable { get { return isTradable == 1; } set { isTradable = Convert.ToInt16(value); } }

                [JsonProperty("marketable")]
                private short isMarketable { get; set; }
                public bool IsMarketable { get { return isMarketable == 1; } set { isMarketable = Convert.ToInt16(value); } }

                [JsonProperty("market_fee_app")]
                public int MarketFeeApp { get; set; }

                [JsonProperty("descriptions")]
                public Description[] Descriptions { get; set; }

                [JsonProperty("actions")]
                public Action[] Actions { get; set; }

                [JsonProperty("owner_actions")]
                public Action[] OwnerActions { get; set; }

                [JsonProperty("tags")]
                public Tag[] Tags { get; set; }

                [JsonProperty("app_data")]
                public App_Data AppData { get; set; }

                public class Description
                {
                    [JsonProperty("type")]
                    public string Type { get; set; }

                    [JsonProperty("value")]
                    public string Value { get; set; }
                }

                public class Action
                {
                    [JsonProperty("name")]
                    public string Name { get; set; }

                    [JsonProperty("link")]
                    public string Link { get; set; }
                }

                public class Tag
                {
                    [JsonProperty("internal_name")]
                    public string InternalName { get; set; }

                    [JsonProperty("name")]
                    public string Name { get; set; }

                    [JsonProperty("category")]
                    public string Category { get; set; }

                    [JsonProperty("color")]
                    public string Color { get; set; }

                    [JsonProperty("category_name")]
                    public string CategoryName { get; set; }
                }

                public class App_Data
                {
                    [JsonProperty("def_index")]
                    public ushort Defindex { get; set; }

                    [JsonProperty("quality")]
                    public int Quality { get; set; }
                }
            }
        }
    }
}
