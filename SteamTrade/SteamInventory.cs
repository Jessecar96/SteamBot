using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade
{

    public class SteamInventory
    {
        public Dictionary<ulong,ItemDescription> descriptions = new Dictionary<ulong,ItemDescription>();
        public Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();

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


        public bool Load(SteamID steamid) 
        {
            dynamic invResponse;
            int i;
            List<int> InvType = new List<int>();
            Item tmpItemData;
            ItemDescription tmpDescription;

            InvType.Add(1);//Gifts (Games), must be public on steam profile in order to work.
            //InvType.Add(...); // ????
            InvType.Add(6);//Trading Cards, Emoticons & Backgrounds.

            try
            {
                for (i = 0; i < InvType.Count;i++ )
                {
                    string response = SteamWeb.Fetch(string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/753/{1}/?trading=1",steamid.ConvertToUInt64(),InvType[i]), "GET", null, null, true);
                    invResponse = JsonConvert.DeserializeObject(response);

                    if (invResponse.success == true)
                    {
                        //rgInventory = Items on Steam Inventory
                        foreach (var item in invResponse.rgInventory)
                        {
                            if (item == null)
                                break;

                            foreach (var itemid in item)//get first item (easy way)
                            {
                                tmpItemData = new Item();
                                tmpItemData.id = itemid.id;
                                tmpItemData.classid = itemid.classid;
                                items.Add((ulong)itemid.id, tmpItemData);
                                break;
                            }
                        }


                        // rgDescriptions = Steam Inventory "Item Schema"
                        foreach (var description in invResponse.rgDescriptions)
                        {
                            foreach (var classid_instanceid in description)// classid + '_' + instenceid
                            {
                                tmpDescription = new ItemDescription();
                                tmpDescription.name = classid_instanceid.name;
                                tmpDescription.type = classid_instanceid.type;

                                descriptions.Add((ulong)classid_instanceid.classid, tmpDescription);
                                break;
                            }
                        }
                    }
                }//end for
            }//end try
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
    }
}
