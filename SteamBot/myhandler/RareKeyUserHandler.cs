using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class RareKeyUserHandler : UserHandler
    {
        int    UserRareAdded  , BotRareAdded, UserKeyAdded , BotKeyAdded ,FakeItem= 0;
        static int BuyPricePerKey = 5;
        static int SellPricePerKey = 6;
        static int CommonExangeRate = 2;
    
        public RareKeyUserHandler(Bot bot, SteamID sid)
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
            BotRareAdded = 0;
            BotKeyAdded = 0;
            UserRareAdded = 0;
            UserKeyAdded = 0;
            FakeItem = 0;
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
           
            Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
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
            //if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity ==null )&& ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            if (item.Defindex ==15003)
            {
                UserKeyAdded++;
                int RareToAdd = UserKeyAdded * BuyPricePerKey;
                if (RareToAdd > BotRareAdded)
                {
                    
                        BotRareAdded =BotRareAdded + AddRare (RareToAdd - BotRareAdded);
                        if (RareToAdd > BotRareAdded)
                        {
                            Trade.SendMessage("机器人稀有不够，请移除一个key");
                        }
                }
                Trade.SendMessage("机器人添加:" + "稀有 " + BotRareAdded + " 用户添加:" + "钥匙 " + UserKeyAdded);
            }
            if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable"  || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserRareAdded++;
                
                int KeyToAdded = UserRareAdded / SellPricePerKey;
                if (KeyToAdded > BotKeyAdded)
                {
                    if (Trade.AddItemByDefindex(15003))
                    {
                        BotKeyAdded++;
                    }
                    else
                    {
                        int UserRareToRemove = UserRareAdded - BotKeyAdded * SellPricePerKey;
                        Trade.SendMessage("我已经没有key了，请移除"+UserRareToRemove +"个稀有");
                    }
                }
                Trade.SendMessage("机器人添加:" + "钥匙 " + BotKeyAdded + " 用户添加:" + "稀有 " + UserRareAdded);
            }
            else
            {
                FakeItem++;
                Trade.SendMessage("你添加了一件我不支持的物品,请移除,否则交易无法完成");//不是卡片则提示用户，不做其他操作   
            }
            
        }
        public int AddRare(int num)
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
            return i;

        }
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            
            var item = Trade.CurrentSchemazh.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
            if (item.Defindex == 15003)
            {
                UserKeyAdded--;
                int RareToAdd = UserKeyAdded * BuyPricePerKey;
                if (RareToAdd < BotRareAdded)
                {
                    for (int i = 0; i < (BotRareAdded - RareToAdd); i++)
                    {
                        Trade.RemoveItemByNotDefindex(15003);
                    }
                        BotRareAdded = RareToAdd;

                }
                Trade.SendMessage("机器人添加:" + "稀有 " + BotRareAdded + " 用户添加:" + "钥匙 " + UserKeyAdded);
            }
            if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserRareAdded--;
                
                int KeyToAdded = UserRareAdded / SellPricePerKey;
                if (KeyToAdded > BotKeyAdded)
                {
                    if (Trade.AddItemByDefindex(15003))
                    {
                        BotKeyAdded--;
                    }
                   
                }
                Trade.SendMessage("机器人添加:" + "钥匙 " + BotKeyAdded + " 用户添加:" + "稀有 " + UserRareAdded);
            }
            else
            {
                FakeItem++;
                Trade.SendMessage("你移除了一件我不支持的物品");//不是卡片则提示用户，不做其他操作   
            }

           
                
                 
            
        }
        
         public override void OnTradeMessage(string message) //根据用户在交易窗口的指令添加及移除卡
        {
            Bot.log.Info("[TRADE MESSAGE] " + message);
            
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
            if (BotKeyAdded < (UserRareAdded / SellPricePerKey))
            {
                int x = UserRareAdded - BotKeyAdded * SellPricePerKey;
                Trade.SendMessage("你放的稀有超过所需的稀有数量,你可以移除"+x+"个稀有");
            }
            if (BotRareAdded <(UserKeyAdded * BuyPricePerKey))
            {
                int x;
                //if ((BotRareAdded % BuyPricePerKey) != 0)
               // {
                //    x = UserKeyAdded - BotRareAdded / BuyPricePerKey + 1;
              //  }
              //  else
               // {
                    x =UserKeyAdded - BotRareAdded / BuyPricePerKey;
              //  }
                Trade.SendMessage("机器人稀有不够,你可以移除" + x + "个key");
            }
            if (IsAdmin || ((BotKeyAdded <=(UserRareAdded /SellPricePerKey ))&&(BotRareAdded <=(UserKeyAdded *BuyPricePerKey))))
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

