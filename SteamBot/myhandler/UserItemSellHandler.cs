using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class UserItemSellHandler : UserHandler
    {
        int    UserRareAdded  , BotRareAdded, UserUncommonAdded , UserCommonAdded = 0;
        //static int Commonvalue = 1;
      //  static int Uncommonvalue = 5;
       // static int Rarevalue = 25;
        //static int CommonExangeRate = 2;
        int UserItemAdded = 0;
        int RareWardNum, UncommonWardNum  = 0;
        int fakeitem = 0;
        //UserItem.Useritem item = null;
        UserItem.Useritem item = new UserItem.Useritem();
        List<UserItem.Useritem> UserItemToAdded = new List<UserItem.Useritem>();
        int PriceKey, PriceRr = 0;
        bool SetingPrice = false;
        int TradeType = 0;
        bool TradeErroe = false;
        static int RateOfKeyAndRare = 5;
        static UserItem currentuseritem = null;
        static long filetime;
        static bool Warding = false;
        public UserItemSellHandler(Bot bot, SteamID sid)
            : base(bot, sid) 
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
          
           PriceKey = 0;
           TradeErroe = false;
           PriceRr = 0;
           TradeType = 0;
           //item = null;
           
           UserItemToAdded.Clear();
           ReInititem();
           item.Steam64id = OtherSID.ConvertToUInt64();
            
             
        }
        public void ReInititem()
        {
            item.Id = 0;
            item.Defindex = 0;
            item.Pricekey = 0;
            item.Pricerr = 0;
            item.Status = 0;

        }


        public override void OnLoginCompleted()
        {
            if (currentuseritem == null)
            {
                currentuseritem =UserItem.FetchSchema();
            }
            
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
           // if (message == ".removeall")
            //{
                // Commenting this out because RemoveAllFriends is a custom function I wrote.
              // Bot.SteamFriends.RemoveAllFriends();
               // Bot.log.Warn("Removed all friends from my friends list.");
           // }
            if (message.Contains("price"))
            {
                string msg = message;
                msg = msg.Remove(0, 5);
                msg = msg.Trim();
                
                UserItem.Useritem yyy = new UserItem.Useritem () ;
                int pricexxx = int.MaxValue ;
                foreach (var xxx in currentuseritem.Items)
                {
                    if (xxx.Item_name == msg && xxx.Status ==0 && (xxx.Pricekey *RateOfKeyAndRare + xxx.Pricerr )<pricexxx )
                    {
                        yyy = xxx;
                        
                    }
                }
                string x;
                if (yyy == null)
                {
                    x = "没有找到 " + msg;
                }
                string x ="物品名称" + yyy.Item_name +   "id   " + yyy.Id +"  "+ yyy.Pricekey + "key   " + yyy.Pricerr + "RR";
                Bot.SteamFriends.SendChatMessage(OtherSID, type, x);
            }
                  
            //Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
        }

        public override bool OnTradeRequest() 
        {
            long timecheck = DateTime.Now.ToFileTime();
            if ((timecheck - filetime )>350000000)
            {
                Warding=false;
            }
            if (Warding == true)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg , "其他人正在操作,30s后重试");
                return false;
            }
            else
            {
                Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
                return true;
            }
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
            Trade.SendMessage("初始化成功.");
        }

        public void AddRare(int num)
        {
            
            var items = new List<Inventory.Item>();
            var dota2item = Trade.Dota2Schema.GetItem(0);
            int i = 0;
            foreach (Inventory.Item item in Trade.MyInventory.Items)
            {
                
                  
                if (i >= num)
                {

                    break;
                }
                 
                else
                {
                   
                    dota2item = Trade.Dota2Schema.GetItem(item.Defindex);
                    
                   
                    if (dota2item != null)
                    {
                        
                        if (dota2item != null && dota2item.Item_rarity == "rare" && dota2item.Prefab == "wearable")
                        {

                            i++;
                            Trade.AddItem(item.Id);
 
                        }
                    }
                }
                
                
            }
            Trade.SendMessage("添加稀有" + i + "件");
            
        }

        public void AddUncommon(int num)
        {

            var items = new List<Inventory.Item>();
            var dota2item = Trade.Dota2Schema.GetItem(0);
            int i = 0;

            foreach (Inventory.Item item in Trade.MyInventory.Items)
            {

                if (i >= num)
                {

                    break;
                }

                else
                {

                    dota2item = Trade.Dota2Schema.GetItem(item.Defindex);
                    if (dota2item != null)
                    {

                        if (dota2item != null && dota2item.Item_rarity == "uncommon" && dota2item.Prefab == "wearable")
                        {

                            i++;
                            Trade.AddItem(item.Id);


                        }
                    }
                }


            }
             Trade.SendMessage("添加罕见" + i + "件");

        }



        public void AddCommon(int num)
        {

            var items = new List<Inventory.Item>();
            var dota2item = Trade.Dota2Schema.GetItem(0);
            int i = 0;
             
            foreach (Inventory.Item item in Trade.MyInventory.Items)
            {
                 
                if (i >= num)
                {

                    break;
                }

                else
                {
                     
                    dota2item = Trade.Dota2Schema.GetItem(item.Defindex);
                     
                    if (dota2item != null)
                    {

                        if (dota2item != null && (dota2item.Item_rarity == "common" || dota2item.Item_rarity == null) && (dota2item.Prefab == "default_item" || dota2item.Prefab == "wearable"))
                        {
                            i++;
                            Trade.AddItem(item.Id);
                        }
                    }
                }
            }
             Trade.SendMessage("添加普通" + i + "件");
        }
          
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            if (inventoryItem == null)
            {
                
                Trade.SendMessage("无法识别物品，请打开仓库显示");
            }
            else
            {

                ReInititem();
                SetingPrice = true;
                item.Id = inventoryItem.Id;
                item.Defindex = inventoryItem.Defindex;
                Trade.SendMessage("请设置物品价格");               
                
            }

            
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            Trade.CancelTrade();
            
            var item = Trade.CurrentSchemazh.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);


            if (dota2item.Item_rarity == "rare" &&  (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin")  )
            {
                UserRareAdded--;
                Trade.SendMessage("用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity == null) && (dota2item.Prefab == "default_item"|| dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserCommonAdded--;
                Trade.SendMessage("用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else if ((dota2item.Item_rarity == "uncommon") && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserUncommonAdded--;
                Trade.SendMessage("用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else
            {
                fakeitem--;
                Trade.SendMessage("你移除了一件我不支持的物品");//不是卡片则提示用户，不做其他操作   
            }
                
                 
            
        }
        
         public override void OnTradeMessage(string message) //根据用户在交易窗口的指令添加及移除卡
        {
           Bot.log.Info("[TRADE MESSAGE] " + message);
           string msg = message.ToLower();
           if (msg.Contains("getbackmyitems"))
           {
               if (TradeType == 0)
               {
                   TradeType = 3;
                   Trade.SendMessage("交易模式已经设定为取回物品模式");
               }
               if (TradeType == 3)
               {
                   {
                       foreach (var xxx in currentuseritem.Items)
                       {
                           if (xxx.Steam64id == OtherSID.ConvertToUInt64() && xxx.Status == 0)
                           {
                               ReInititem();
                               Trade.SendMessage("添加物品 original_id = " + xxx.Id);
                               if (!Trade.AddItemByOriginal_id(xxx.Id))
                               {
                                   Trade.SendMessage("添加物品 original_id = " + xxx.Id +" 物品失败，请向管理员提交bug");
                               }
                               item = xxx;
                               item.Status = 3;
                               UserItemToAdded.Add(item);
                           }
                       }
                   }
               }
               else
               {
                   Trade.SendMessage("当前处于其他模式，不能取回物品，请重新交易");
               }
           }
           else if (msg.Contains("additem"))
           {
               if (TradeType == 0)
               {
                   TradeType = 1;
                   Trade.SendMessage("交易模式已经设定为购买模式");
               }
 
           }
           else if (msg.Contains("additembyid"))
           {
               if (TradeType == 0)
               {
                   TradeType = 1;
                   Trade.SendMessage("交易模式已经设定为购买模式");
               }
           }
           else if (TradeType == 2 && SetingPrice == true)
           {
               if (msg.Contains("setpricekey"))
               {
                   msg = msg.Remove(0, 11);
                   msg = msg.Trim();
                   PriceKey = Convert.ToInt32(msg);
                   Trade.SendMessage(PriceKey + "key " + PriceRr + "RR");

               }
               else if (msg.Contains("setpricerr"))
               {
                   msg = msg.Remove(0, 10);
                   msg = msg.Trim();
                   PriceRr = Convert.ToInt32(msg);
                   Trade.SendMessage(PriceKey + "key " + PriceRr + "RR");
               }
               else if (msg.Contains("save"))
               {
                   item.Pricekey = PriceKey;
                   item.Pricerr = PriceRr;
                   Trade.SendMessage(item.Id + " 价格 " + PriceKey + "key " + PriceRr + "RR" + " 已保存");
                   UserItemToAdded.Add(item);
                   SetingPrice = false;
               }
               else
               {
                   Trade.SendMessage("错误的指令");
               }
           }
           else
           {
               Trade.SendMessage("错误的指令或者未设定交易模式");
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
                
                if (Validate())
                {
                    
                    Bot.log.Success("User is ready to trade!");
                    Trade.SetReady(true);
                    
                }
                else
                {
                    Trade.SendMessage("你提供的有我不支持的物品");
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
                    TradeErroe = true;
                    //准备检查库存
                }
                Log.Success("Trade Complete!");
                OnTradeClose();
                if (TradeType == 2)
                {
                    Additemstofile();
                }
                else if (TradeType == 1)
                {
                    Sellitemsfromfile();
                }
                else if (TradeType == 3)
                {
                    ToRemoveitemsfromfile();
                }
                else
                {
                    Bot.log.Warn("Tradetype is 0");
                }


                
                
            }
            else
            {
                Trade.SetReady(false);
            }
            
            
        }

        public  void Additemstofile()
        {
            foreach (var xxx in UserItemToAdded)
            {
                currentuseritem.Items.Add(xxx);
            }
            // 写入文件；

        }
        public void Sellitemsfromfile()
        {
            foreach (var xxx in UserItemToAdded)
            {
                int x = 0;
                    foreach (var yyy in currentuseritem.Items )
                    {
                        if (yyy.Id ==xxx.Id && yyy.Steam64id ==xxx.Steam64id && yyy.Defindex ==xxx.Defindex  &&yyy.Status !=3)
                        {
                            x = currentuseritem.Items.IndexOf(yyy);
                            currentuseritem.Items[x].Status = 1;
                        }
                    }
            }
            // 写入文件；

        }

        public void ToRemoveitemsfromfile()
        {
            foreach (var xxx in UserItemToAdded)
            {
                int x = 0;
                foreach (var yyy in currentuseritem.Items)
                {
                    if (yyy.Id == xxx.Id && yyy.Steam64id == xxx.Steam64id && yyy.Defindex == xxx.Defindex &&   yyy.Status==0 )
                    {
                        x = currentuseritem.Items.IndexOf(yyy);
                        currentuseritem.Items[x].Status = 3;
                    }
                }
            }
            // 写入文件；

        }


        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            //Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate ()
        {
           

                return true;
          
            
        }
        
    }
 
}

