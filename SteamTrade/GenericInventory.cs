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
        private static Dictionary<ulong, List<KeyValuePair<int, long>>> InventoriesToFetch = new Dictionary<ulong, List<KeyValuePair<int, long>>>();
        private static Dictionary<ulong, CookieContainer> Cookies = new Dictionary<ulong, CookieContainer>();
        private Dictionary<int, Dictionary<long, Inventory>> inventories = new Dictionary<int, Dictionary<long, Inventory>>();
        private Dictionary<int, Dictionary<long, Task>> InventoryTasks = new Dictionary<int, Dictionary<long, Task>>();
        private Task ConstructTask = null;
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

        public GenericInventory(SteamID steamId, SteamID botId, bool inTrade = false)
        {
            ConstructTask = Task.Factory.StartNew(() =>
            {
                if (inTrade)
                {
                    foreach (var pair in InventoriesToFetch[botId])
                    {
                        int appId = pair.Key;
                        long contextId = pair.Value;
                        if (contextId == 0) continue;
                        if (!InventoryTasks.ContainsKey(appId))
                            InventoryTasks[appId] = new Dictionary<long, Task>();
                        InventoryTasks[appId][contextId] = Task.Factory.StartNew(() =>
                        {
                            string inventoryUrl = string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/", steamId.ConvertToUInt64(), appId, contextId);
                            Inventory inventory = FetchForeignInventory(steamId, botId, appId, contextId);
                            if (!inventories.ContainsKey(appId))
                                inventories[appId] = new Dictionary<long, Inventory>();
                            if (inventory != null && !inventories[appId].ContainsKey(contextId))
                                inventories[appId].Add(contextId, inventory);
                        });
                    }
                }
                else
                {
                    string baseInventoryUrl = "http://steamcommunity.com/profiles/" + steamId.ConvertToUInt64() + "/inventory/";
                    string response = RetryWebRequest(baseInventoryUrl, botId);
                    Regex reg = new Regex("var g_rgAppContextData = (.*?);");
                    Match m = reg.Match(response);
                    if (m.Success)
                    {
                        try
                        {
                            string json = m.Groups[1].Value;
                            var schemaResult = JsonConvert.DeserializeObject<Dictionary<int, InventoryApps>>(json);
                            foreach (var app in schemaResult)
                            {
                                int appId = app.Key;
                                if (!ShouldFetchInventory(appId, botId)) continue;
                                InventoryTasks[appId] = new Dictionary<long, Task>();
                                foreach (var contextId in app.Value.RgContexts.Keys)
                                {
                                    InventoryTasks[appId][contextId] = Task.Factory.StartNew(() =>
                                    {
                                        string inventoryUrl = string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/", steamId.ConvertToUInt64(), appId, contextId);
                                        var inventory = FetchInventory(inventoryUrl, steamId, botId, appId, contextId);
                                        if (!inventories.ContainsKey(appId))
                                            inventories[appId] = new Dictionary<long, Inventory>();
                                        if (inventory != null && !inventories[appId].ContainsKey(contextId))
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
                }
            });
        }

        public enum InventoryTypes
        {
            TF2,
            Dota2,
            Portal2,
            CSGO,
            SpiralKnights,
            SteamGifts,
            SteamCoupons,
            SteamCommunity,
            SteamItemRewards
        };

        public static bool ShouldFetchInventory(int appId, ulong botId)
        {
            if (InventoriesToFetch.ContainsKey(botId))
            {
                foreach (var inventory in InventoriesToFetch[botId])
                {
                    if (inventory.Key == appId) return true;
                }
            }
            return false;
        }

        public static void AddInventoriesToFetch(InventoryTypes type, SteamID botId)
        {
            switch (type)
            {
                case InventoryTypes.TF2:
                    AddInventoriesToFetch(440, 2, botId);
                    break;
                case InventoryTypes.Dota2:
                    AddInventoriesToFetch(570, 2, botId);
                    break;
                case InventoryTypes.Portal2:
                    AddInventoriesToFetch(620, 2, botId);
                    break;
                case InventoryTypes.CSGO:
                    AddInventoriesToFetch(730, 2, botId);
                    break;
                case InventoryTypes.SpiralKnights:
                    AddInventoriesToFetch(99900, 0, botId);
                    break;
                case InventoryTypes.SteamGifts:
                    AddInventoriesToFetch(753, 1, botId);
                    break;
                case InventoryTypes.SteamCoupons:
                    AddInventoriesToFetch(753, 3, botId);
                    break;
                case InventoryTypes.SteamCommunity:
                    AddInventoriesToFetch(753, 6, botId);
                    break;
                case InventoryTypes.SteamItemRewards:
                    AddInventoriesToFetch(753, 7, botId);
                    break;
            }
        }

        public static void AddInventoriesToFetch(int appId, long contextId, ulong botId)
        {
            if (!InventoriesToFetch.ContainsKey(botId))
                InventoriesToFetch[botId] = new List<KeyValuePair<int, long>>();
            InventoriesToFetch[botId].Add(new KeyValuePair<int, long>(appId, contextId));
        }

        public static void SetCookie(CookieContainer cookies, ulong botId)
        {
            Cookies[botId] = cookies;
        }

        private string GetSessionId(SteamID botId)
        {
            var cookies = Cookies[botId].GetCookies(new Uri("http://steamcommunity.com"));
            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name == "sessionid")
                {
                    return cookie.Value;
                }
            }
            return "";
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

        public void AddForeignInventory(SteamID steamId, SteamID botId, int appId, long contextId)
        {
            Inventory inventory = FetchForeignInventory(steamId, botId, appId, contextId);
            if (!inventories.ContainsKey(appId))
                inventories[appId] = new Dictionary<long, Inventory>();
            if (inventory != null && !inventories[appId].ContainsKey(contextId))
                inventories[appId].Add(contextId, inventory);
        }

        private Inventory FetchForeignInventory(SteamID steamId, SteamID botId, int appId, long contextId)
        {
            string inventoryUrl = string.Format("http://steamcommunity.com/trade/{0}/foreigninventory/?sessionid={1}&steamid={2}&appid={3}&contextid={4}", steamId.ConvertToUInt64(), GetSessionId(botId), steamId.ConvertToUInt64(), appId, contextId);
            return FetchInventory(inventoryUrl, steamId, botId, appId, contextId);
        }

        private Inventory FetchInventory(string inventoryUrl, SteamID steamId, SteamID botId, int appId, long contextId)
        {
            string response = RetryWebRequest(inventoryUrl, botId);
            try
            {
                var inventory = JsonConvert.DeserializeObject<Inventory>(response);
                foreach (var element in inventory.RgInventory)
                {
                    var item = element.Value;
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
                foreach (var element in inventory.RgCurrencies)
                {
                    var item = element.Value;
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
                Console.WriteLine("Failed to deserialize {0}.", inventoryUrl);
                Console.WriteLine(ex);
                return null;
            }
        }

        public Inventory.Item GetItem(int appId, long contextId, ulong id, bool isCurrency)
        {
            try
            {
                var inventory = Inventories[appId];
                if (isCurrency)
                {
                    Inventory.CurrencyItem currencyItem = null;
                    if (inventory[contextId].RgCurrencies.ContainsKey(id.ToString()))
                        currencyItem = inventory[contextId].RgCurrencies[id.ToString()];
                    var key = string.Format("{0}_{1}", currencyItem.ClassId, 0);
                    return inventory[contextId].RgDescriptions[key];
                }
                else
                {
                    Inventory.ItemInfo item = null;
                    if (inventory[contextId].RgInventory.ContainsKey(id.ToString()))
                        item = inventory[contextId].RgInventory[id.ToString()];
                    var key = string.Format("{0}_{1}", item.ClassId, item.InstanceId);
                    return inventory[contextId].RgDescriptions[key];
                }
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
        private string RetryWebRequest(string url, SteamID botId)
        {
            for (int i = 0; i < WEB_REQUEST_MAX_RETRIES; i++)
            {
                try
                {
                    return SteamWeb.Fetch(url, "GET", null, Cookies[botId]);
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

        public bool Success = true;
        public bool IsPrivate = false;

        public class GenericItem
        {
            public int AppId { get; private set; }
            public long ContextId { get; private set; }
            public ulong ItemId { get; private set; }
            public int Amount { get; private set; }
            public bool IsCurrency { get; private set; }

            public GenericItem(int appId, long contextId, ulong itemId, int amount, bool isCurrency)
            {
                this.AppId = appId;
                this.ContextId = contextId;
                this.ItemId = itemId;
                this.Amount = amount;
                this.IsCurrency = isCurrency;
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
            private dynamic _rgInventory { get; set; }
            private Dictionary<string, ItemInfo> rgInventory { get; set; }
            public Dictionary<string, ItemInfo> RgInventory
            {
                // for some games rgInventory will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        if (rgInventory == null)
                            rgInventory = JsonConvert.DeserializeObject<Dictionary<string, ItemInfo>>(Convert.ToString(_rgInventory));
                        return rgInventory;
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
            private dynamic _rgCurrency { get; set; }
            private Dictionary<string, CurrencyItem> rgCurrencies { get; set; }
            public Dictionary<string, CurrencyItem> RgCurrencies
            {
                // for some games rgCurrency will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        if (rgCurrencies == null)
                            rgCurrencies = JsonConvert.DeserializeObject<Dictionary<string, CurrencyItem>>(Convert.ToString(_rgCurrency));
                        return rgCurrencies;
                    }
                    catch
                    {
                        return new Dictionary<string, CurrencyItem>();
                    }
                }
                set
                {
                    rgCurrencies = value;
                }
            }

            [JsonProperty("rgDescriptions")]
            private dynamic _rgDescriptions { get; set; }
            private Dictionary<string, Item> rgDescriptions { get; set; }
            public Dictionary<string, Item> RgDescriptions
            {
                get
                {
                    try
                    {
                        if (rgDescriptions == null)
                            rgDescriptions = JsonConvert.DeserializeObject<Dictionary<string, Item>>(Convert.ToString(_rgDescriptions));
                        return rgDescriptions;
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
                /// <summary>
                /// Not available in Inventory History
                /// </summary>
                public int Amount { get; set; }
                /// <summary>
                /// Not available in Inventory History
                /// </summary>
                public bool IsCurrency { get; set; }

                [JsonProperty("id")]
                public ulong Id { get; set; }

                /// <summary>
                /// Only available in Inventory History
                /// </summary>
                [JsonProperty("owner")]
                public ulong OwnerId { get; set; }

                [JsonProperty("pos")]
                public int Position { get; set; }

                [JsonProperty("contextid")]
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
                public string DisplayName { get; set; }

                [JsonProperty("market_hash_name")]
                public string MarketHashName { get; set; }

                [JsonProperty("market_name")]
                private string name { get; set; }
                public string Name
                {
                    get
                    {
                        if (string.IsNullOrEmpty(name))
                            return DisplayName;
                        return name;
                    }
                }

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

                [JsonProperty("commodity")]
                private short isCommodity { get; set; }
                public bool IsCommodity { get { return isCommodity == 1; } set { isCommodity = Convert.ToInt16(value); } }

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