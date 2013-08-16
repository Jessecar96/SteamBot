using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class WardUserHandler : UserHandler
    {
        int    UserRareAdded  , BotRareAdded, UserUncommonAdded , UserCommonAdded = 0;
        //static int Commonvalue = 1;
      //  static int Uncommonvalue = 5;
       // static int Rarevalue = 25;
        //static int CommonExangeRate = 2;
        int UserItemAdded = 0;
        int[] UserItem = new int[10];
        int RareWardNum, UncommonWardNum  = 0;
        int fakeitem = 0;
        static long filetime;
        string[] UserItemRarity = new string[10];
        string[] WardResult = new string[10];
        static bool Warding = false;
        public WardUserHandler(Bot bot, SteamID sid)
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
            
            if (Warding == true)
            {
                Trade.SendMessage("添加物品中");
                Warding = false;
                UserUncommonAdded = 0;
                UserCommonAdded = 0;
                UserRareAdded = 0;
                //RareWardNum = 1;
                AddRare(RareWardNum );
                RareWardNum = 0;
                AddUncommon(UncommonWardNum);
                UncommonWardNum = 0;
                BotRareAdded = 0;
                //UserCommonAdded = 0;
                //UserUncommonAdded = 0;
                
                fakeitem = 0;
                Warding = false;
            }
            else
            {
                BotRareAdded = 0;
                //UserCommonAdded = 0;
                //UserUncommonAdded = 0;
                RareWardNum = 0;
                UncommonWardNum=0;
                UserUncommonAdded =0;
                UserCommonAdded =0;
                UserRareAdded = 0;
                Warding = false;
                fakeitem = 0;

            }
             
        }


        public override void OnLoginCompleted()
        {
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
            
                  
            Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
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


        }



        public void AddCommon(int num)
        {

            var items = new List<Inventory.Item>();
            var dota2item = Trade.Dota2Schema.GetItem(0);
            int i = 0;
            Bot.log.Warn("XXX");
            foreach (Inventory.Item item in Trade.MyInventory.Items)
            {
                Bot.log.Warn(item.Defindex.ToString());
                if (i >= num)
                {

                    break;
                }

                else
                {
                    Bot.log.Warn(item.Defindex.ToString());
                    dota2item = Trade.Dota2Schema.GetItem(item.Defindex);
                    Bot.log.Warn("2" + item.Defindex.ToString());
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
            //if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity ==null )&& ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))

            if (dota2item.Item_rarity == "rare" && ( dota2item.Prefab == "wearable"  || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin") )
            {
                UserRareAdded++;

                Trade.SendMessage("用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity == null) && (dota2item.Prefab == "default_item" ||dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserCommonAdded++;
                Trade.SendMessage("用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else if ((dota2item.Item_rarity == "uncommon") && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserUncommonAdded++;
                Trade.SendMessage("用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else
            {
                fakeitem++;
                Trade.SendMessage("你添加了一件我不支持的物品");//不是卡片则提示用户，不做其他操作   
            }
            
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            
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
           message = message.ToLower();
           if (IsAdmin)
           {

               string msg = message;
               if (message.Contains("addrare"))
               {
                   msg = msg.Remove(0, 7);
                   msg = msg.Trim();
                   int num = Convert.ToInt32(msg);
                   AddRare(num);
                   

               }
               else if (message.Contains("adduncommon"))
               {
                   msg = msg.Remove(0, 11);
                   msg = msg.Trim();
                   int num = Convert.ToInt32(msg);
                   AddUncommon(num);
                   
               }
               else if (message.Contains("addcommon"))
               {
                   msg = msg.Remove(0, 9);
                   msg = msg.Trim();
                   int num = Convert.ToInt32(msg);
                   AddCommon(num);

               }
               else
               {
                   Trade.SendMessage("错误的命令");
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
                Ward();
                
                
            }
            else
            {
                Trade.SetReady(false);
            }
            
            
        }

        public  void Ward()
        {
            if (Warding == true)
            {
                RareWardNum = 0;
                UncommonWardNum = 0;
                Random ro = new Random();
                UInt64 xxx = OtherSID.ConvertToUInt64();
                string logx = xxx.ToString();
                string y = "";
                for (int i = 0; i < UserRareAdded; i++)
                {
                    int x = ro.Next(0, 100);
                    
                    if (0<=x && x<=39)
                    {
                        y = "2rare";
                        RareWardNum++;
                        RareWardNum++;
                    }
                    else
                    {
                        y = "no";
                    }
                    logx = logx + "   " + y;
                }

                
              
                for (int i = 0; i < UserUncommonAdded; i++)
                {
                    int x = ro.Next(0, 100);

                    if (0 <= x && x < 16)
                    {
                        y = "1rare";
                        RareWardNum++;
                        
                    }
                    else
                    {
                        y = "no";
                    }
                    logx = logx + "   " + y;
                }

                for (int i = 0; i < UserCommonAdded; i++)
                {
                    int x = ro.Next(0, 100);

                    if (0 <= x && x < 20)
                    {
                        y = "1uncommon";
                        UncommonWardNum++;

                    }
                    else
                    {
                        y = "no";
                    }
                    logx = logx + "   " + y;
                }
                Log.Success(logx);
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, logx);
                if (RareWardNum > 0 || UncommonWardNum>0)
                {
                    Warding  = true;
                    filetime = DateTime.Now.ToFileTime();
                    Bot.OpenTrade(OtherSID);
                }
                else
                {
                    Warding = false;
                }
            }
        }


        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            //Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate ()
        {
            if (fakeitem > 0)
            {

                return false;
            }
            else
            {
                if (UserRareAdded > 0 || UserCommonAdded >0 || UserUncommonAdded >0)
                {

                    Warding = true;

                }
                else
                {

                    Warding = false;

                }

                return true;
            }
            
        }
        
    }
 
}

