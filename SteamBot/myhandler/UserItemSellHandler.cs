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
        List<UserItem.Useritem> UserItemToAdded;
        int PriceKey, PriceRr = 0;
        bool SetingPrice = false;
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
            Bot.log.Warn("1");
           PriceKey = 0;
           Bot.log.Warn("2");
           PriceRr = 0;
           Bot.log.Warn("3");
           item = null;
           Bot.log.Warn("4");
           UserItemToAdded.Clear();
           Bot.log.Warn("5");
            
             
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
                message = message.Remove(0, 5);
                message = message.Trim();
                ulong xid = Convert.ToUInt64(message);
                UserItem.Useritem yyy = null ;
                
                foreach (var xxx in currentuseritem.Items)
                {
                    if (xxx.Id == xid)
                        yyy=xxx;
                }
                string x = "steamid   " + yyy.Id +"  "+ yyy.Pricekey + "key   " + yyy.Pricerr + "RR";
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
                Trade.SendMessage("请设置物品价格");

                SetingPrice = true;
                UInt64 xxxx = OtherSID.ConvertToUInt64();

                item.Id = inventoryItem.Id;
                
                Bot.log.Warn("4");
                
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
           if (SetingPrice == true)
           {
               if (msg.Contains("setpricekey"))
               {
                   msg = msg.Remove(0, 11);
                   msg = msg.Trim();
                   PriceKey = Convert.ToInt32(msg);

               }
               else if (msg.Contains("setpricerr"))
               {
                   msg = msg.Remove(0, 10);
                   msg = msg.Trim();
                   PriceRr = Convert.ToInt32(msg);
               }
               else if (msg.Contains("save"))
               {
                   item.Pricekey = PriceKey;
                   item.Pricerr = PriceRr;
                   UserItemToAdded.Add(item);
                   SetingPrice = false;
               }
               else
               {
                   Trade.SendMessage("错误的指令");
               }
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
                }
                Log.Success("Trade Complete!");
                OnTradeClose();
                Additemstofile();
                
                
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

