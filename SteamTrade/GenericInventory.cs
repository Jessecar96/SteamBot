using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade
{
    public class GenericInventory
    {
        public Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
        public Dictionary<ulong, ItemDescription> descriptions = new Dictionary<ulong, ItemDescription>();

        public bool loaded = false;
        public string appId;
        public List<string> errors = new List<string>();

        public class Item
        {
            public ulong id{get;set;}
            public ulong classid { get; set; }
            public string contextid { get; set; }
        }

        public class ItemDescription
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool tradable { get; set; }
            public bool marketable { get; set; }

            public dynamic metadata { get; set; }
        }

        public ItemDescription getInfo(ulong id)
        {
            try
            {
                return descriptions[items[id].classid];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errors.Add("getInfo(" + id + ")" + e.Message);
                return new ItemDescription();
            }
        }

        public bool itemExists(ulong id)
        {
            try
            {
                GenericInventory.Item tmpItem = items[id];
                return true;
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
                return false;
            }
        }

        public bool load(ulong appid,List<uint> types, SteamID steamid)
        {
            dynamic invResponse;
            Item tmpItemData;
            ItemDescription tmpDescription;

            loaded = false;
            appId = appid.ToString();

            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    string response = SteamWeb.Fetch(string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/?trading=1", steamid.ConvertToUInt64(),appid, types[i]), "GET", null, null, true);

                    invResponse = JsonConvert.DeserializeObject(response);

                    if (invResponse.success == false)
                    {
                        errors.Add("Fail to open backpack: " + invResponse.Error);
                        break;
                    }

                    //rgInventory = Items on Steam Inventory 
                    foreach (var item in invResponse.rgInventory)
                    {

                        foreach (var itemId in item)
                        {
                            tmpItemData = new Item();
                            tmpItemData.id = itemId.id;
                            tmpItemData.classid = itemId.classid;
                            tmpItemData.contextid = types[i].ToString();

                            items.Add((ulong)itemId.id, tmpItemData);
                            break;
                        }
                    }

                    // rgDescriptions = Item Schema (sort of)
                    foreach (var description in invResponse.rgDescriptions)
                    {
                        foreach (var classid_instanceid in description)// classid + '_' + instenceid 
                        {
                            tmpDescription = new ItemDescription();
                            tmpDescription.name = classid_instanceid.name;
                            tmpDescription.type = classid_instanceid.type;
                            tmpDescription.marketable = (bool) classid_instanceid.marketable;
                            tmpDescription.tradable = (bool) classid_instanceid.marketable;

                            tmpDescription.metadata = classid_instanceid.descriptions;

                            descriptions.Add((ulong)classid_instanceid.classid, tmpDescription);
                            break;
                        }
                    }

                    if (errors.Count > 0)
                        return false;
                    
                }//end for (inventory type)
            }//end try
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errors.Add("Exception: " + e.Message);
                return false;
            }
            loaded = true;
            return true;
        }
    }
}
