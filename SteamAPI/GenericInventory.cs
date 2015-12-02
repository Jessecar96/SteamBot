using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;
using System.Text.RegularExpressions;
using System.Net;

namespace SteamAPI
{
    /// <summary>
    /// Generic Steam Backpack Interface
    /// </summary>
    public class GenericInventory
    {
        private BotInventories inventories = new BotInventories();
        private InventoryTasks InventoryTasks = new InventoryTasks();
        private static Dictionary<ulong, CookieContainer> Cookies = new Dictionary<ulong, CookieContainer>();
        private Task ConstructTask = null;
        private const int WEB_REQUEST_MAX_RETRIES = 3;
        private const int WEB_REQUEST_TIME_BETWEEN_RETRIES_MS = 1000;
        private SteamWeb steamWeb;

        /// <summary>
        /// Gets the content of all inventories listed in http://steamcommunity.com/profiles/STEAM_ID/inventory/
        /// </summary>
        public BotInventories Inventories
        {
            get
            {
                WaitAllTasks();
                return inventories;
            }
        }

        public GenericInventory(SteamID steamId, SteamID botId, SteamWeb steamWeb)
        {
            ConstructTask = Task.Factory.StartNew(() =>
            {
                this.steamWeb = steamWeb;
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
                            InventoryTasks[appId] = new InventoryTasks.ContextTask();
                            foreach (var contextId in app.Value.RgContexts.Keys)
                            {
                                InventoryTasks[appId][contextId] = Task.Factory.StartNew(() =>
                                {
                                    string inventoryUrl = string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/", steamId.ConvertToUInt64(), appId, contextId);
                                    var inventory = FetchInventory(inventoryUrl, steamId, botId, appId, contextId);
                                    if (!inventories.HasAppId(appId))
                                        inventories[appId] = new BotInventories.ContextInventory();
                                    if (inventory != null && !inventories[appId].HasContextId(contextId))
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

        public static GenericInventory FetchInventories(SteamID steamId, SteamID botId, SteamWeb steamWeb)
        {
            return new GenericInventory(steamId, botId, steamWeb);
        }  

        public enum AppID
        {
            TF2 = 440,
            Dota2 = 570,
            Portal2 = 620,
            CSGO = 730,
            SpiralKnights = 99900,
            Steam = 753
        };

        /// <summary>
        /// Use this to iterate through items in the inventory.
        /// </summary>
        /// <param name="appId">App ID</param>
        /// <param name="contextId">Context ID</param>
        /// <returns>A List containing GenericInventory.Inventory.Item elements</returns>
        public List<GenericInventory.Inventory.Item> GetInventory(int appId, ulong contextId)
        {
            return Inventories[appId][contextId].RgInventory.Values.ToList();
        }

        public void AddForeignInventory(SteamID steamId, SteamID botId, int appId, ulong contextId)
        {
            Inventory inventory = FetchForeignInventory(steamId, botId, appId, contextId);
            if (!inventories.HasAppId(appId))
                inventories[appId] = new BotInventories.ContextInventory();
            if (inventory != null && !inventories[appId].HasContextId(contextId))
                inventories[appId].Add(contextId, inventory);
        }

        private Inventory FetchForeignInventory(SteamID steamId, SteamID botId, int appId, ulong contextId)
        {
            string inventoryUrl = string.Format("http://steamcommunity.com/trade/{0}/foreigninventory/?sessionid={1}&steamid={2}&appid={3}&contextid={4}", steamId.ConvertToUInt64(), steamWeb.SessionId, steamId.ConvertToUInt64(), appId, contextId);
            return FetchInventory(inventoryUrl, steamId, botId, appId, contextId);
        }

        private Inventory FetchInventory(string inventoryUrl, SteamID steamId, SteamID botId, int appId, ulong contextId, int start = 0)
        {
            string response = RetryWebRequest(inventoryUrl + "&start=" + start, botId);
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
                    inventoryItem.IsCurrency = false;
                }
                foreach (var element in inventory.RgCurrencies)
                {
                    var item = element.Value;
                    var classId = item.ClassId;
                    var instanceId = 0;
                    var key = string.Format("{0}_{1}", classId, instanceId);
                    var inventoryItem = inventory.RgDescriptions[key];
                    inventoryItem.ContextId = contextId;
                    inventoryItem.IsCurrency = item.IsCurrency;
                }
                if(inventory.More){
                    Inventory addInv = FetchInventory(inventoryUrl, steamId, botId, appId, contextId, inventory.MoreStart);
                    inventory.RgInventory.Concat(addInv.RgInventory);
                    inventory.RgCurrencies.Concat(addInv.RgCurrencies);
                    inventory.RgDescriptions.Concat(addInv.RgDescriptions);
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

        public Inventory.Item GetItem(int appId, ulong contextId, ulong id)
        {
            try
            {
                var inventory = Inventories[appId];
                if (inventory[contextId].RgInventory.ContainsKey(id.ToString()))
                    return inventory[contextId].RgInventory[id.ToString()];               
            }
            catch
            {
                
            }
            return null;
        }

        public Inventory.CurrencyItem GetCurrencyItem(int appId, ulong contextId, ulong id)
        {
            try
            {
                var inventory = Inventories[appId];
                if (inventory[contextId].RgCurrencies.ContainsKey(id.ToString()))
                    return inventory[contextId].RgCurrencies[id.ToString()];
            }
            catch
            {

            }
            return null;
        }

        public Inventory.ItemDescription GetItemDescription(int appId, ulong contextId, ulong id, bool isCurrency)
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
                    Inventory.Item item = null;
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

        public Inventory.ItemDescription GetItemDescriptionByClassId(int appId, ulong contextId, ulong classId, bool isCurrency)
        {
            try
            {
                var inventory = Inventories[appId];
                if (isCurrency)
                {
                    var key = string.Format("{0}_{1}", classId, 0);
                    return inventory[contextId].RgDescriptions[key];
                }
                else
                {
                    Inventory.Item item = null;
                    foreach (var rgItem in inventory[contextId].RgInventory)
                    {
                        if (rgItem.Value.ClassId == classId)
                            item = inventory[contextId].RgInventory[rgItem.Key];
                    }
                    var key = string.Format("{0}_{1}", classId, item.InstanceId);
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
            OnInventoriesLoaded(EventArgs.Empty);
        }

        public delegate void InventoriesLoadedEventHandler(object sender, EventArgs e);

        // An event that clients can use to be notified whenever the
        // elements of the list change.
        public event InventoriesLoadedEventHandler InventoriesLoaded;

        // Invoke the Changed event; called whenever list changes
        protected virtual void OnInventoriesLoaded(EventArgs e)
        {
            if (InventoriesLoaded != null)
                InventoriesLoaded(this, e);
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
                    return steamWeb.Fetch(url, "GET");
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

        public class GenericItem : IEquatable<GenericItem>
        {
            public int AppId { get; private set; }
            public ulong ContextId { get; private set; }
            public ulong ItemId { get; private set; }
            public int Amount { get; private set; }
            public bool IsCurrency { get; private set; }

            public GenericItem(int appId, ulong contextId, ulong itemId, int amount, bool isCurrency)
            {
                this.AppId = appId;
                this.ContextId = contextId;
                this.ItemId = itemId;
                this.Amount = amount;
                this.IsCurrency = isCurrency;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as GenericItem);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public bool Equals(GenericItem other)
            {
                if (other == null)
                    return false;

                return (this.AppId == other.AppId &&
                        this.ContextId == other.ContextId &&
                        this.ItemId == other.ItemId &&
                        this.Amount == other.Amount &&
                        this.IsCurrency == other.IsCurrency);
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
            public Dictionary<ulong, RgContext> RgContexts { get; set; }

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
            private Dictionary<string, Item> rgInventory { get; set; }
            public Dictionary<string, Item> RgInventory
            {
                // for some games rgInventory will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        if (rgInventory == null)
                            rgInventory = JsonConvert.DeserializeObject<Dictionary<string, Item>>(Convert.ToString(_rgInventory));
                        return rgInventory;
                    }
                    catch
                    {
                        return new Dictionary<string, Item>();
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
            private Dictionary<string, ItemDescription> rgDescriptions { get; set; }
            public Dictionary<string, ItemDescription> RgDescriptions
            {
                get
                {
                    try
                    {
                        if (rgDescriptions == null)
                            rgDescriptions = JsonConvert.DeserializeObject<Dictionary<string, ItemDescription>>(Convert.ToString(_rgDescriptions));
                        return rgDescriptions;
                    }
                    catch
                    {
                        return new Dictionary<string, ItemDescription>();
                    }
                }
                set
                {
                    rgDescriptions = value;
                }
            }

            [JsonProperty("more")]
            public bool More { get; set; }

            //If the JSON returns false it will be 0 (as it should be)
            [JsonProperty("more_start")]
            private dynamic moreStart { get; set; }
            public int MoreStart
            {
                get
                {
                    if (More)
                    {
                        return (int)moreStart;
                    }
                    else
                    {
                        return 0;
                    }
                }
                set
                {
                    moreStart = value;
                }
            }

            public class Item
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

                /// <summary>
                /// Only available in Inventory History
                /// </summary>
                [JsonProperty("owner")]
                public ulong OwnerId { get; set; }
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

            public class ItemDescription
            {
                /// <summary>
                /// Not available in Inventory History
                /// </summary>
                public bool IsCurrency { get; set; }

                /// <summary>
                /// Only available in Inventory History
                /// </summary>
                [JsonProperty("owner")]
                public ulong OwnerId { get; set; }

                [JsonProperty("contextid")]
                public ulong ContextId { get; set; }

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

    public class InventoriesToFetch : Dictionary<SteamID, List<InventoriesToFetch.InventoryInfo>>
    {
        public class InventoryInfo
        {
            public int AppId { get; set; }
            public ulong ContextId { get; set; }

            public InventoryInfo(int appId, ulong contextId)
            {
                this.AppId = appId;
                this.ContextId = contextId;
            }
        }
    }

    public class BotInventories : Dictionary<int, BotInventories.ContextInventory>
    {
        public bool HasAppId(int appId)
        {
            return this.ContainsKey(appId);
        }

        public class ContextInventory : Dictionary<ulong, GenericInventory.Inventory>
        {
            public ulong ContextId { get; set; }
            public GenericInventory.Inventory Inventory { get; set; }

            public bool HasContextId(ulong contextId)
            {
                return this.ContainsKey(contextId);
            }
        }
    }

    public class InventoryTasks : Dictionary<int, InventoryTasks.ContextTask>
    {
        public bool HasAppId(int appId)
        {
            return this.ContainsKey(appId);
        }

        public class ContextTask : Dictionary<ulong, Task>
        {
            public ulong ContextId { get; set; }
            public Task InventoryTask { get; set; }
        }
    }
}