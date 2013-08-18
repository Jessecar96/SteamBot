using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade
{
    public class GenericInventory
    {
        public Dictionary<ulong,ItemDescription> descriptions = new Dictionary<ulong,ItemDescription>();
        public Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
        public bool loaded = false;

        public class Item
        {
            public ulong id{get;set;}
            public ulong classid { get; set; }
        }

        public class ItemDescription
        {
            public string name { get; set; }
            public string type { get; set; }
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
                return null;
            }
        }

        public bool load(ulong appid,List<uint> types, SteamID steamid)
        {
            dynamic invResponse;
            Item tmpItemData;
            ItemDescription tmpDescription;

            loaded = false;
            /*items = new Dictionary<ulong, Item>();
            descriptions = new Dictionary<ulong, ItemDescription>();*/

            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    string response = SteamWeb.Fetch(string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/?trading=1", steamid.ConvertToUInt64(),appid, types[i]), "GET", null, null, true);
                    invResponse = JsonConvert.DeserializeObject(response);

                    if (invResponse.success == false)
                    {
                        return false;
                    }
                    
                    foreach (var item in invResponse.rgInventory)
                    {

                        foreach (var noidea in item)
                        {
                            tmpItemData = new Item();
                            tmpItemData.id = noidea.id;
                            tmpItemData.classid = noidea.classid;

                            //Console.WriteLine(string.Format("ID: {0} - Class: {1}",noidea.id,noidea.classid));

                            items.Add((ulong)noidea.id, tmpItemData);
                            break;
                        }
                    }
                    
                    foreach (var description in invResponse.rgDescriptions)
                    {
                        foreach (var noidea in description)
                        {
                            tmpDescription = new ItemDescription();
                            tmpDescription.name = noidea.name;
                            tmpDescription.type = noidea.type;

                            //Console.WriteLine(string.Format("Class: {0} - Name: {1} - Type: {2}", noidea.classid,noidea.name,noidea.type));

                            descriptions.Add((ulong)noidea.classid, tmpDescription);
                            break;
                        }
                    }
                    
                    

                }//end for
            }//end try
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            loaded = true;
            return true;
        }
    }
}
