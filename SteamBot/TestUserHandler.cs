using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;
using System.Net;
using System.IO;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SteamBot
{
    public class TestUserHandler : UserHandler
    {
        int BotUncommonAdded, UserUncommonAdded, userRareAdded = 0;
    
        public TestUserHandler(Bot bot, SteamID sid)
            : base(bot, sid) 
        {
        }
        public override void OnTradeSuccess()
        {

        }
        public override bool OnFriendAdd () 
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") added me!");
            // Using a timer here because the message will fail to send if you do it too quickly
            
            return true;
        }
        public void ReInit()
        {
            BotUncommonAdded = 0;
            UserUncommonAdded = 0;
            userRareAdded = 0;
        }


        public override void OnLoginCompleted()
        {
            //const string SchemaMutexName = "steam_bot_dota2";
            string url = Trade.CurrentSchema.ItemsGameUrl;
            string outputpath = "dota2file.json";
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Host = "media.steampowered.com";
            request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
            request.Referer = "http://media.steampowered.com";
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            DateTime schemaLastModified = DateTime.Parse(response.Headers["Last-Modified"]);
            Log.Warn(schemaLastModified.ToString());
            Stream result = response.GetResponseStream();
            string xxx = result.ToString();
            Log.Warn(xxx);
            //response.Close();
            //request.Abort();
            Log.Warn ( Convertvdf2json(result, outputpath, false));

        }
        public static string Convertvdf2json(Stream inputstream, string outputpath, bool CompactJSON)
        {

                using (FileStream stream2 = System.IO.File.Create(outputpath))
                {
                    KeyValue kv = new KeyValue(null, null);
                    
                    kv.ReadAsText(inputstream);
                    return Convert(kv, stream2, CompactJSON);
                }

        }
        public static string Convert(KeyValue kv, Stream outputStream, bool compactJSON)
        {
            JObject obj2 = new JObject();
            new JObject();
            obj2[kv.Name] = ConvertRecursive(kv);
            using (StreamWriter writer = new StreamWriter (outputStream ,Encoding.UTF8  ))
            {
                using (JsonTextWriter writer2 = new JsonTextWriter(writer))
                {
                    writer2.Formatting = compactJSON ? Formatting.None : Formatting.Indented;
                    obj2.WriteTo(writer2, new JsonConverter[0]);
                    //string x;
                    string x = obj2["items_game"]["items"].ToString ();

                    //JObject xxx = JObject.Parse(x);
                    return x;
                }
            }
        }

        private static JToken ConvertRecursive(KeyValue kv)
        {
            JObject obj2 = new JObject();
            if (kv.Children.Count <= 0)
            {
                return kv.Value;
            }
            foreach (KeyValue value2 in kv.Children)
            {
                obj2[value2.Name] = ConvertRecursive(value2);
            }
            return obj2;
        }


        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override void OnFriendRemove () 
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") removed me!");
        }
        
        public override void OnMessage (string message, EChatEntryType type) 
        {


            Bot.log.Info("[TRADE MESSAGE] " + message);
            //message = message.ToLower();
            string msg = message;
            if (message.Contains("set"))
            {
                msg = msg.Remove(0, 3);
                msg = msg.Trim();
                var set = Trade.CurrentSchemazh.GetItemBySet(msg);
                foreach (string x in set.Setsinclude)
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              x + "          ");
                }
            }
            else if (message.Contains("stock"))
            {
                string strmessage = message;
                msg = msg.Remove(0, 5);
                msg = msg.Trim();
                var item = Trade.CurrentSchemazh.GetItemByZhname(msg);
                //var dota2item = Trade.Dota2Schema.GetItem(item.Defindex);
                if (item == null)
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "错误的物品名称");
                }
                else
                {
                    Inventory messageInventory = Inventory.FetchInventory(Bot.SteamUser.SteamID.ConvertToUInt64(), Bot.apiKey);
                    List<Inventory.Item> items = messageInventory.GetItemsByDefindex(item.Defindex);
                    if (items.Count != 0)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "机器人库存有 " + strmessage);
                    }

                    else
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "机器人库存没有 " + strmessage);
                    }
                }

            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
            }
        }

        public override bool OnTradeRequest() 
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
            return true;
        }
        
        public override void OnTradeError (string error) 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, 
                                              EChatEntryType.ChatMsg,
                                              "Oh, there was an error: " + error + "."
                                              );
            Bot.log.Warn (error);
        }
        
        public override void OnTradeTimeout () 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info ("User was kicked because he was AFK.");
        }
        
        public override void OnTradeInit() 
        {
            ReInit();
            //TradeCountInventory(true);
            Trade.SendMessage("初始化成功.请用 add+空格+物品名称 来添加物品， remove+空格+物品名称 来移除物品");
        }
        
        
          
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
            /*if (dota2item.Item_set == null)
            {
                Trade.SendMessage("null");
            }
            else if (dota2item.Item_set == "")
            {
                Trade.SendMessage("空字符串");
            }
            else
            {
                Trade.SendMessage(dota2item.Item_set);
            }
            */
            Trade.SendMessage(dota2item.Item_rarity + "   " + item.Defindex + "   " + dota2item.Prefab );
            if (dota2item.Item_rarity == "uncommon" && ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserUncommonAdded++;
                Trade.SendMessage("机器人添加:" + "罕见 " + BotUncommonAdded + " 用户添加:" + "罕见 " + UserUncommonAdded + " 稀有 " + userRareAdded);
            }
            else if (dota2item.Item_rarity == "rare" && !(dota2item.Name.Contains("Taunt")) && !(dota2item.Name.Contains("Treasure")) && dota2item.Defindex !=10066)
            {
                userRareAdded++;
                Trade.SendMessage("机器人添加:" + "罕见 " + BotUncommonAdded + " 用户添加:" + "罕见 " + UserUncommonAdded + " 稀有 " + userRareAdded);
            }
            else
            {
                Trade.SendMessage("你添加了一件我不支持的物品,我只支持套装散件的罕见及稀有 ");//不是卡片则提示用户，不做其他操作   
            }
            
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            
            var item = Trade.CurrentSchemazh.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);

            if (dota2item.Item_rarity == "uncommon" && ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                    UserUncommonAdded --;
                    Trade.SendMessage("机器人添加:" + "罕见 " + BotUncommonAdded + " 用户添加:" + "罕见 " + UserUncommonAdded + " 稀有 " + userRareAdded);
                }
                else if (dota2item.Item_rarity == "rare" && !(dota2item.Name.Contains("Taunt")) && !(dota2item.Name.Contains("Treasure")) && dota2item.Defindex != 10066)
                {
                    userRareAdded--;
                    Trade.SendMessage("机器人添加:" + "罕见 " + BotUncommonAdded + " 用户添加:" + "罕见 " + UserUncommonAdded + " 稀有 " + userRareAdded);
                }
                else
                {
                    Trade.SendMessage("你移除了一件我不支持的物品 ");//不是卡片则提示用户，不做其他操作   
                }
                
                 
            
        }
        
         public override void OnTradeMessage(string message) //根据用户在交易窗口的指令添加及移除卡
        {
            Bot.log.Info("[TRADE MESSAGE] " + message);
            //message = message.ToLower();
            string msg = message;
            if (message.Contains("add"))
            {
                msg = msg.Remove(0, 3);
                msg = msg.Trim();
                var item = Trade.CurrentSchemazh.GetItemByZhname(msg);
                var dota2item = Trade.Dota2Schema.GetItem(item.Defindex );
                if (item == null)
                {
                    Trade.SendMessage("错误的物品名称");
                }
                else
                {
                    if (dota2item.Item_rarity == "uncommon" && dota2item.Prefab == "wearable")
                    {

                        if (Trade.AddItemByDefindex(item.Defindex))
                        {
                            BotUncommonAdded++;
                        }
                        else
                        {
                            Trade.SendMessage("我没有 " + msg);
                        }
                    }
                    else
                    {
                        Trade.SendMessage("这个机器人只支持交换罕见物品");
                    }
                }

            }

            else if (message.Contains("remove"))
            {
                msg = msg.Remove(0, 6);
                msg = msg.Trim();
                var item = Trade.CurrentSchemazh.GetItemByZhname(msg);
                if (item == null)
                {
                    Trade.SendMessage("错误的物品名称");
                }
                else
                {

                    if (Trade.RemoveItemByDefindex(item.Defindex))
                    {
                        BotUncommonAdded--;
                    }
                    else
                    {
                        Trade.SendMessage("机器人没有添加 " + msg);
                    }
                }

            }
            else
            {
                Trade.SendMessage("请用 add+空格+物品名称 来添加物品， remove+空格+物品名称 来移除物品");
            }
        }
        
        public override void OnTradeReady (bool ready) 
        {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else 
            {
                Bot.log.Success("User is ready to trade!");
                if (Validate())
                {
                    Trade.SetReady(true);
                }
                else
                {
                    Trade.SendMessage("你添加的罕见必须大于或者机器人添加的罕见的2倍");
                    Trade.SetReady(false);
                }

            }
           
                    
               
         
        }
        
        public override void OnTradeAccept() 
        {
           
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
            if (Validate())
            {
                try
                {
                    Trade.AcceptTrade();
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
                }
                Log.Success("Trade Complete!");
            }
            else
            {
                Trade.SetReady(false);
            }
           

            OnTradeClose ();
        }
        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            //Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate ()
        {

            if (IsAdmin || (BotUncommonAdded *3 <= UserUncommonAdded + userRareAdded *5))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
    }
 
}

