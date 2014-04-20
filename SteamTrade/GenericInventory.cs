﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;
using SteamTrade.TradeWebAPI;
using System.Text.RegularExpressions;

namespace SteamTrade
{
    /// <summary>
    /// Generic Steam Backpack Interface
    /// </summary>
    public class GenericInventory
    {
        Dictionary<int, Dictionary<long, Inventory>> inventories = new Dictionary<int, Dictionary<long, Inventory>>();

        public GenericInventory(SteamID steamId)
        {
            string inventoryUrl = "http://steamcommunity.com/profiles/" + steamId.ConvertToUInt64() + "/inventory/";
            string response = SteamWeb.Fetch(inventoryUrl, "GET");
            Regex reg = new Regex("var g_rgAppContextData = (.*?);");
            Match m = reg.Match(response);
            string json = m.Groups[1].Value;
            try
            {
                var schemaResult = JsonConvert.DeserializeObject<Dictionary<int, InventoryApps>>(json);
                List<Task> tasks = new List<Task>();
                foreach (var app in schemaResult)
                {
                    int appId = app.Key;
                    foreach (var context in app.Value.RgContexts)
                    {
                        long contextId = context.Key;
                        tasks.Add(Task.Factory.StartNew(() => {
                            var inventory = GetInventory(appId, contextId, steamId) ?? new Inventory();
                            if (!inventories.ContainsKey(appId))
                                inventories[appId] = new Dictionary<long, Inventory>();
                            inventories[appId].Add(contextId, inventory);
                        }));
                    }
                }
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Test()
        {
            Console.WriteLine("{0} inventories loaded.\r\n", inventories.Count);
            foreach (var app in inventories)
            {
                Console.WriteLine("AppId {0} has {1} contexts.", app.Key, app.Value.Count);
                foreach (var context in app.Value)
                {
                    Console.WriteLine("\tContext {0} has {1} items.", context.Key, context.Value.RgInventory.Count);
                    try
                    {
                        var item = context.Value.RgInventory.First().Value;
                        var key = string.Format("{0}_{1}", item.ClassId, item.InstanceId);
                        var inventoryItem = context.Value.RgDescriptions[key];
                        Console.WriteLine("\t\tFirst item in context {0}: {1}", context.Key, inventoryItem.Name);
                    }
                    catch
                    {
                        Console.WriteLine("\t\tFirst item in context {0}: N/A", context.Key);
                    }
                }
            }
        }

        private Inventory GetInventory(int appId, long contextId, SteamID steamId)
        {
            string inventoryUrl = string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/", steamId.ConvertToUInt64(), appId, contextId);
            string response = SteamWeb.Fetch(inventoryUrl, "GET");
            try
            {
                return JsonConvert.DeserializeObject<Inventory>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to deserialize {0}:{1}.", appId, contextId);
                Console.WriteLine(ex);
                return null;
            }
        }

        public Inventory.RgDescription GetItem(int appId, long contextId, ulong id)
        {
            try
            {
                var inventory = inventories[appId];
                var item = inventory[contextId].RgInventory[id.ToString()];
                var key = string.Format("{0}_{1}", item.ClassId, item.InstanceId);
                var inventoryItem = inventory[contextId].RgDescriptions[key];
                return inventoryItem;
            }
            catch
            {
                return null;
            }
        }

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
            public Dictionary<string, RgInventoryItems> RgInventory
            {
                // for some games rgInventory will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, RgInventoryItems>>(Convert.ToString(rgInventory));
                        return dictionary;
                    }
                    catch
                    {
                        return new Dictionary<string, RgInventoryItems>();
                    }
                }
                set
                {
                    rgInventory = value;
                }
            }

            [JsonProperty("rgCurrency")]
            private dynamic rgCurrency { get; set; }
            public Dictionary<string, RgCurrency> RgCurrencies
            {
                // for some games rgCurrency will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, RgCurrency>>(Convert.ToString(rgCurrency));
                        return dictionary;
                    }
                    catch
                    {
                        return new Dictionary<string, RgCurrency>();
                    }
                }
                set
                {
                    rgCurrency = value;
                }
            }

            [JsonProperty("rgDescriptions")]
            private dynamic rgDescriptions { get; set; }
            public Dictionary<string, RgDescription> RgDescriptions
            {
                // for some games rgDescriptions will be an empty array instead of a dictionary (e.g. [])
                // this try-catch handles that
                get
                {
                    try
                    {
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, RgDescription>>(Convert.ToString(rgDescriptions));
                        return dictionary;
                    }
                    catch
                    {
                        return new Dictionary<string, RgDescription>();
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

            public class RgInventoryItems
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

            public class RgCurrency
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

            public class RgDescription
            {
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
                    public int Defindex { get; set; }

                    [JsonProperty("quality")]
                    public int Quality { get; set; }
                }
            }
        }
    }
}
