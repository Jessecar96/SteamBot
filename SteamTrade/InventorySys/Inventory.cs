using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace SteamTrade.InventorySys
{
    public sealed class Inventory :
        IEquatable<Inventory>,
        IEquatable<InventoryType>,
        IEquatable<Game>,
        IEquatable<uint>,
        IEnumerable<Item>,
        IEnumerable<ulong>
    {
        private readonly ItemEnumerator itemEnumerator;

        public readonly SteamID Owner;

        public readonly InventoryType InventoryType;

        public readonly IEnumerable<Item> Items;

        internal Inventory(SteamID owner, InventoryType inventoryType, IEnumerable<Item> items)
        {
            Owner = owner;
            InventoryType = inventoryType;
            Items = items;
            itemEnumerator = new ItemEnumerator(items);
        }

        #region Overrides
        public override int GetHashCode() => Owner.GetHashCode() * InventoryType.GetHashCode() * Items.GetHashCode<Item>();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Inventory other = obj as Inventory;
            if ((object)other == null)
                return false;
            return Owner == other.Owner &&
                InventoryType == other.InventoryType &&
                Items.Equals<Item>(other.Items);
        }
        #endregion

        #region Implementations of IEquatable<>
        public bool Equals(Inventory other) => Equals((object)other);

        public bool Equals(InventoryType other) => InventoryType == other;

        public bool Equals(Game other) => InventoryType == other;

        public bool Equals(uint other) => (uint)InventoryType == other;
        #endregion

        #region Implementations of IEnumerable<>
        IEnumerator<Item> IEnumerable<Item>.GetEnumerator() => itemEnumerator;

        IEnumerator<ulong> IEnumerable<ulong>.GetEnumerator() => itemEnumerator;

        IEnumerator IEnumerable.GetEnumerator() => itemEnumerator;
        #endregion

        #region Equality operators
        public static bool operator ==(Inventory inventory1, Inventory inventory2)
        {
            if (ReferenceEquals(inventory1, inventory2))
                return true;
            else if ((object)inventory1 == null || (object)inventory2 == null)
                return false;
            else
                return inventory1.Equals(inventory2);
        }

        public static bool operator !=(Inventory inventory1, Inventory inventory2) => !(inventory1 == inventory2);

        public static bool operator ==(Inventory inventory, InventoryType inventoryType)
        {
            if ((object)inventory == null && inventoryType == null)
                return true;
            else if ((object)inventory == null || inventoryType == null)
                return false;
            else
                return inventory.Equals(inventoryType);
        }

        public static bool operator !=(Inventory inventory, InventoryType inventoryType) => !(inventory == inventoryType);

        public static bool operator ==(InventoryType inventoryType, Inventory inventory) => inventory == inventoryType;

        public static bool operator !=(InventoryType inventoryType, Inventory inventory) => !(inventoryType == inventory);

        public static bool operator ==(Inventory inventory, Game game)
        {
            if ((object)inventory == null)
                return false;
            else
                return inventory.Equals(game);
        }

        public static bool operator !=(Inventory inventory, Game game) => !(inventory == game);

        public static bool operator ==(Game game, Inventory inventory) => inventory == game;

        public static bool operator !=(Game game, Inventory inventory) => !(game == inventory);

        public static bool operator ==(Inventory inventory, uint game)
        {
            if ((object)inventory == null)
                return false;
            else
                return inventory.Equals(game);
        }

        public static bool operator !=(Inventory inventory, uint game) => !(inventory == game);

        public static bool operator ==(uint game, Inventory inventory) => inventory == game;

        public static bool operator !=(uint game, Inventory inventory) => !(game == inventory);
        #endregion

        public delegate void OnInventoryFetched(Inventory inventory);

        #region Inventory fetching static methods
        /// <summary>
        /// Fetches all supported inventories of SteamTrade
        /// </summary>
        /// <param name="steamWeb">Steamweb instance to fetch with</param>
        /// <param name="owner">Who's inventory to fetch</param>
        /// <param name="onInventoryFetched">Callback to fire on inventory fetch task successful completion</param>
        /// <param name="fetchType">How should we obtain <paramref name="owner"/>'s inventory</param>
        public static void FetchSupportedInventories(SteamWeb steamWeb, SteamID owner, OnInventoryFetched onInventoryFetched, FetchType fetchType = FetchType.Normal)
        {
            foreach (InventoryType inventoryType in InventoryType.SupportedInventories)
                FetchInventory(steamWeb, owner, inventoryType, fetchType).ContinueWith(inventoryTask =>
                {
                    if (inventoryTask.IsFaulted)
                        ExceptionDispatchInfo.Capture(inventoryTask.Exception.InnerException).Throw();
                    onInventoryFetched(inventoryTask.Result);
                });
        }

        private static Task<Inventory> FetchInventory(SteamWeb steamWeb, SteamID owner, InventoryType inventoryType, FetchType fetchType = FetchType.Normal, uint start = 0)
        {
            string url, referer;
            NameValueCollection data;
            switch (fetchType)
            {
                case FetchType.Trade:
                    url = "http://" + SteamWeb.SteamCommunityDomain + "/trade/" + (ulong)owner + "/foreigninventory";
                    referer = "http://" + SteamWeb.SteamCommunityDomain + "/trade/" + (ulong)owner;
                    data = new NameValueCollection
                    {
                        { "sessionid", steamWeb.SessionId },
                        { "steamid", ((ulong)owner).ToString() },
                        { "appid", ((uint)inventoryType).ToString() },
                        { "contextid", inventoryType.ContextId.ToString() }
                    };
                    break;
                case FetchType.TradeOffer:
                    url = "http://" + SteamWeb.SteamCommunityDomain + "/tradeoffer/new/partnerinventory";
                    referer = null;
                    data = new NameValueCollection
                    {
                        { "sessionid", steamWeb.SessionId },
                        { "partner", ((ulong)owner).ToString() },
                        { "appid", ((uint)inventoryType).ToString() },
                        { "contextid", inventoryType.ContextId.ToString() }
                    };
                    break;
                default:
                    url = "http://" + SteamWeb.SteamCommunityDomain + "/profiles/" + (ulong)owner + "/inventory/json/" + (uint)inventoryType + "/" + inventoryType.ContextId;
                    referer = "http://" + SteamWeb.SteamCommunityDomain + "/profiles/" + (ulong)owner;
                    data = null;
                    break;
            }
            return steamWeb.FetchAsync(url, WebRequestMethods.Http.Get, data, referer: referer).ContinueWith(fetchTask =>
            {
                if (fetchTask.IsFaulted)
                    ExceptionDispatchInfo.Capture(fetchTask.Exception.InnerException).Throw();
                JObject invJson = JObject.Parse(fetchTask.Result);
                if (!(bool)invJson["success"])
                    return null;
                List<Item> items = new List<Item>();
                foreach (JProperty itemProperty in invJson["rgInventory"])
                {
                    JObject itemJson = itemProperty.Value.ToObject<JObject>();
                    JObject descriptionJson = (JObject)invJson["rgDescriptions"][itemJson["classid"] + "_" + itemJson["instanceid"]];
                    List<string> descriptions = null;
                    if (descriptionJson["descriptions"].Type == JTokenType.Array)
                    {
                        descriptions = new List<string>();
                        foreach (JObject description in descriptionJson["descriptions"])
                            descriptions.Add((string)description["value"]);
                    }
                    List<ItemTag> tags = null;
                    if (descriptionJson["tags"] != null && descriptionJson["tags"].Type == JTokenType.Array)
                    {
                        tags = new List<ItemTag>();
                        foreach (JObject tag in descriptionJson["tags"])
                            tags.Add(new ItemTag()
                            {
                                InternalName = (string)tag["internal_name"],
                                Name = (string)tag["name"],
                                Category = (string)tag["category"],
                                CategoryName = (string)tag["category_name"]
                            });
                    }
                    ItemAppData appData = null;
                    if (descriptionJson["app_data"] != null && descriptionJson["app_data"].Type == JTokenType.Object)
                    {
                        JObject dataJO = (JObject)descriptionJson["app_data"];
                        appData = new ItemAppData()
                        {
                            AppId = (uint)dataJO["appid"],
                            ItemType = (uint)dataJO["item_type"],
                            Denomination = (uint)dataJO["denomination"],
                            DefIndex = (uint)dataJO["def_index"],
                            Quality = (uint)dataJO["quality"]
                        };
                    }
                    items.Add(new Item()
                    {
                        InventoryType = inventoryType,
                        Id = (ulong)itemJson["id"],
                        Name = (string)itemJson["name"],
                        OriginalName = (string)itemJson["market_name"],
                        Type = (string)itemJson["type"],
                        IsTradable = (uint)itemJson["tradable"] == 1,
                        IsCommodity = (uint)itemJson["commodity"] == 1,
                        Descriptions = descriptions,
                        Tags = tags,
                        AppData = appData
                    });
                }
                if ((bool)invJson["more"])
                    Task.Run(() => FetchInventory(steamWeb, owner, inventoryType, fetchType, (uint)invJson["more_start"]).ContinueWith(inventoryTask =>
                    {
                        if (inventoryTask.IsFaulted)
                            ExceptionDispatchInfo.Capture(inventoryTask.Exception.InnerException).Throw();
                        Inventory inventory = inventoryTask.Result;
                        if ((object)inventory == null)
                            return;
                        items.AddRange(inventory);
                    })).Wait();
                if (items.Count == 0)
                    return null;
                return new Inventory(owner, inventoryType, items);
            });
        }

        /// <summary>
        /// Fetches a single supported inventory of SteamTrade
        /// </summary>
        /// <param name="steamWeb">Steamweb instance to fetch with</param>
        /// <param name="owner">Who's inventory to fetch</param>
        /// <param name="inventoryType">What inventory to fetch</param>
        /// <param name="fetchType">How should we obtain <paramref name="owner"/>'s inventory</param>
        public static Task<Inventory> FetchInventory(SteamWeb steamWeb, SteamID owner, InventoryType inventoryType, FetchType fetchType = FetchType.Normal) => FetchInventory(steamWeb, owner, inventoryType, fetchType, 0);
        #endregion
    }
}
