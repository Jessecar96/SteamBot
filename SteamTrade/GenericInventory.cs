using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public Dictionary<ulong, Item> items
        {
            get
            {
                if(_loadTask == null)
                    return null;
                _loadTask.Wait();
                return _items;
            }
        }

        public Dictionary<string, ItemDescription> descriptions
        {
            get
            {
                if(_loadTask == null)
                    return null;
                _loadTask.Wait();
                return _descriptions;
            }
        }

        public List<string> errors
        {
            get
            {
                if(_loadTask == null)
                    return null;
                _loadTask.Wait();
                return _errors;
            }
        }

        public bool isLoaded = false;

        private Task _loadTask;
        private Dictionary<string, ItemDescription> _descriptions = new Dictionary<string, ItemDescription>();
        private Dictionary<ulong, Item> _items = new Dictionary<ulong, Item>();
        private List<string> _errors = new List<string>();

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
        }

        public ItemDescription getDescription(ulong id)
        {
            if(_loadTask == null)
                return null;
            _loadTask.Wait();

            try
            {
                return _descriptions[_items[id].descriptionid];
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("ERROR: getDescription({0}) >> {1}",id,e.Message));
                return null;
            }
        }

        public void load(int appid, IEnumerable<long> contextIds, SteamID steamid)
        {
            List<long> contextIdsCopy = contextIds.ToList();
            _loadTask = Task.Factory.StartNew(() => loadImplementation(appid, contextIdsCopy, steamid));
        }

        public void loadImplementation(int appid, IEnumerable<long> contextIds, SteamID steamid)
        {
            dynamic invResponse;
            isLoaded = false;
            Dictionary<string, string> tmpAppData;

            _items.Clear();
            _descriptions.Clear();
            _errors.Clear();

            try
            {
                foreach(long contextId in contextIds)
                {
                    string response = SteamWeb.Fetch(string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/", steamid.ConvertToUInt64(), appid, contextId), "GET", null, null, true);
                    invResponse = JsonConvert.DeserializeObject(response);

                    if (invResponse.success == false)
                    {
                        _errors.Add("Fail to open backpack: " + invResponse.Error);
                        continue;
                    }

                    //rgInventory = Items on Steam Inventory 
                    foreach (var item in invResponse.rgInventory)
                    {

                        foreach (var itemId in item)
                        {
                            _items.Add((ulong)itemId.id, new Item()
                            {
                                appid = appid,
                                contextid = contextId,
                                assetid = itemId.id,
                                descriptionid = itemId.classid + "_" + itemId.instanceid
                            });
                            break;
                        }
                    }

                    // rgDescriptions = Item Schema (sort of)
                    foreach (var description in invResponse.rgDescriptions)
                    {
                        foreach (var class_instance in description)// classid + '_' + instenceid 
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

                            _descriptions.Add("" + (class_instance.classid??'0') + "_" + (class_instance.instanceid??'0'), 
                                new ItemDescription()
                                    {
                                        name = class_instance.name,
                                        type = class_instance.type,
                                        marketable = (bool) class_instance.marketable,
                                        tradable = (bool)class_instance.tradable,
                                        app_data = tmpAppData
                                    }
                            );
                            break;
                        }
                    }
                    
                }//end for (contextId)
            }//end try
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                _errors.Add("Exception: " + e.Message);
            }
            isLoaded = true;
        }
    }
}
