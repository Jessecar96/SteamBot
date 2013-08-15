using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class WardUserHandler : UserHandler
    {
        int    UserRareAdded  , BotRareAdded= 0;
        //static int Commonvalue = 1;
      //  static int Uncommonvalue = 5;
       // static int Rarevalue = 25;
        //static int CommonExangeRate = 2;
        int UserItemAdded = 0;
        int[] UserItem = new int[10];
        int RareWardNum = 0;
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
                Warding = false;
                for (int i = 0; i < RareWardNum; i++)
                {
                    Add2Rare(RareWardNum );
                }
                RareWardNum = 0;
                BotRareAdded = 0;
                //UserCommonAdded = 0;
                //UserUncommonAdded = 0;
                UserRareAdded = 0;
                Warding = false;
            }
            else
            {
                BotRareAdded = 0;
                //UserCommonAdded = 0;
                //UserUncommonAdded = 0;
                RareWardNum = 0;
                UserRareAdded = 0;
                Warding = false;
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

        public void Add2Rare(int num)
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

                    if (dota2item != null && dota2item.Item_rarity == "rare")
                    {
                        i++;
                        Bot.log.Success("T1");
                        Bot.log.Success(Trade.myOfferedItems.Count.ToString());
                        Bot.log.Success(i);
                        Bot.log.Success(item.Id.ToString());
                        Trade.AddItem(item.Id);
                        Bot.log.Success("T2");
                        Bot.log.Success(Trade.myOfferedItems.Count.ToString());

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
                
                Trade.SendMessage("机器人添加:" + "稀有 " + BotRareAdded + " 用户添加:" + "稀有 " + UserRareAdded);
            }
            else
            {
                Trade.SendMessage("你添加了一件我不支持的物品");//不是卡片则提示用户，不做其他操作   
            }
            
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            
            var item = Trade.CurrentSchemazh.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);


            if (dota2item.Item_rarity == "rare" && ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight") && !dota2item.Model_player.Contains("morphling")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserRareAdded--;
                Trade.SendMessage("机器人添加:" + "稀有 " + BotRareAdded + " 用户添加:" + "稀有 " + UserRareAdded);
            }
            else
            {
                Trade.SendMessage("你移除了一件我不支持的物品");//不是卡片则提示用户，不做其他操作   
            }
                
                 
            
        }
        
         public override void OnTradeMessage(string message) //根据用户在交易窗口的指令添加及移除卡
        {
            Bot.log.Info("[TRADE MESSAGE] " + message);
            //message = message.ToLower();
            

           
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
                Bot.log.Success("X1");
                Bot.log.Success(Trade.myOfferedItems.Count.ToString() );
                Bot.log.Success(Trade.steamMyOfferedItems.Count.ToString());
                Bot.log.Success("X2");
                if (Validate())
                {
                    Bot.log.Success("6");
                    Bot.log.Success("User is ready to trade!");
                    Trade.SetReady(true);
                    Bot.log.Success("7");
                }
                else
                {
                    Trade.SendMessage("你添加的稀有必须大于等于机器人添加的稀有的"  + "倍");
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
                Random ro = new Random();

                for (int i = 0; i < UserRareAdded; i++)
                {
                    int x = ro.Next(0, 100);
                    
                    if (0<=x && x<=100)
                    {
                        WardResult[i] = "2rare";
                        RareWardNum++;
                        RareWardNum++;
                    }
                    else
                    {
                        WardResult[i] = "no";
                    }
                }
                UInt64 xxx = OtherSID.ConvertToUInt64();
                string logx = xxx.ToString();
                for (int i = 0; i < UserRareAdded; i++)
                {
                    logx = logx + "   " + WardResult[i];
                }
                Log.Success(logx);
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, logx);
                if (RareWardNum > 0)
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

            if (UserRareAdded > 0)
            {
                Bot.log.Success("1");
                Warding = true;
                Bot.log.Success("2");
            }
            else
            {
                Bot.log.Success("3");
                Warding = false;
                Bot.log.Success("4");
            }
            Bot.log.Success("5");
                return true;
            
        }
        
    }
 
}

