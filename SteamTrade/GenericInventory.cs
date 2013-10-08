using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SteamKit2;
using SteamTrade.TradeWebAPI;

namespace SteamTrade
{
    /// <summary>
    /// Generic Steam Backpack Interface
    /// </summary>
    public class GenericInventory
    {
        public Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
        public Dictionary<string, ItemDescription> descriptions = new Dictionary<string, ItemDescription>();

        public bool isLoaded = false;
        public List<string> errors = new List<string>();

        public class Item : TradeUserAssets
        {
            public string descriptionid { get; set; }

            public override string  ToString()
            {
                return string.Format("id:{0}, appid:{1}, contextid:{2}, amount:{3}, descriptionid:{4}",
                    assetid, appid, contextid, amount, descriptionid);
            }
        }

        public class ItemDescription
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool tradable { get; set; }
            public bool marketable { get; set; }

            public Dictionary<string, string> app_data{ get; set; }

            public void debug_app_data()
            {
                Console.WriteLine("\n\""+name+"\"");
                if (app_data == null)
                {
                    Console.WriteLine("Doesn't have app_data");
                    return;
                }

                foreach (var value in app_data)
                {
                    Console.WriteLine(string.Format("{0} = {1}",value.Key,value.Value));
                }
                Console.WriteLine("");
            }

            public override string ToString()
            {
                return string.Format("Name:{0}, Type:{1}, Tradeable:{2}, Marketable:{3}",
                    name,type,tradable,marketable);
            }
        }

        public ItemDescription getDescription(ulong id)
        {
            try
            {
                return descriptions[items[id].descriptionid];
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("ERROR: getDescription({0}) >> {1}",id,e.Message));
                return null;
            }
        }

        public bool load(int appid,List<int> contextIds, SteamID steamid)
        {
            dynamic invResponse;
            isLoaded = false;
            Dictionary<string, string> tmpAppData;
            items.Clear();
            descriptions.Clear();

            try
            {
                for (int i = 0; i < contextIds.Count; i++)
                {
                    string response = SteamWeb.Fetch(string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/", steamid.ConvertToUInt64(),appid, contextIds[i]), "GET", null, null, true);
                    invResponse = JsonConvert.DeserializeObject(response);

                    if (invResponse.success == false)
                    {
                        errors.Add("Fail to open backpack: " + invResponse.Error);
                        continue;
                    }

                    //rgInventory = Items on Steam Inventory 
                    foreach (var item in invResponse.rgInventory)
                    {

                        foreach (var itemId in item)
                        {
                            items.Add((ulong)itemId.id, new Item()
                            {
                                appid = appid,
                                contextid = contextIds[i],
                                assetid = itemId.id,
                                descriptionid = itemId.classid + "_" + itemId.instanceid
                            });
                            break;
                        }
                    }

                    // rgDescriptions = Item Schema (sort of)
                    foreach (var description in invResponse.rgDescriptions)
                    {
                        foreach (var class_instance in description)// classid + '_' + instanceid 
                        {
                            if (class_instance.app_data != null)
                            {
                                tmpAppData = new Dictionary<string, string>();
                                foreach (var value in class_instance.app_data)
                                {
                                    tmpAppData.Add(""+value.Name,""+value.Value);
                                }
                            }
                            else
                            {
                                tmpAppData= null;
                            }

                            descriptions.Add("" + (class_instance.classid ?? '0') + "_" + (class_instance.instanceid ?? '0'),
                                new ItemDescription()
                                    {
                                        name = class_instance.name,
                                        type = class_instance.type,
                                        marketable = (bool)class_instance.marketable,
                                        tradable = (bool)class_instance.tradable,
                                        app_data = tmpAppData
                                    }
                            );
                            break;
                        }
                    }

                    if (errors.Count > 0)
                        return false;
                    
                }//end for (contextId)
            }//end try
            catch (Exception e)
            {
                Console.WriteLine(string.Format("ERROR: load({0},{1},{2}) >> {3}",appid,contextIds.ToString(),steamid.ConvertToUInt64(), e.Message));
                errors.Add("Exception: load(...) >> " + e.Message);
                return false;
            }
            isLoaded = true;
            return true;
        }
    }
}
