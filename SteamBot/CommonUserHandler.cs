using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class CommonUserHandler : UserHandler
    {
        int   UserUncommonAdded, UserRareAdded , UserCommonAdded , BotCommonAdded= 0;
        static int Commonvalue = 1;
        static int Uncommonvalue = 5;
        static int Rarevalue = 25;
        static int CommonExangeRate = 2;
    
        public CommonUserHandler(Bot bot, SteamID sid)
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
            BotCommonAdded = 0;
            UserCommonAdded = 0;
            UserUncommonAdded = 0;
            UserRareAdded = 0;
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
            //if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity ==null )&& ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity == null) && ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight") && !dota2item.Model_player.Contains("morphling")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserCommonAdded++;
                Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
             else if ((dota2item.Item_rarity == "uncommon" ) && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin" ))
            {
                 UserUncommonAdded++;
                 Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
             }
             else if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserRareAdded++;
                Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
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

            if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity == null) && ((dota2item.Prefab == "wearable" && dota2item.Item_set != null && !dota2item.Model_player.Contains("axe") && !dota2item.Model_player.Contains("witchdoctor") && !dota2item.Model_player.Contains("omniknight") && !dota2item.Model_player.Contains("morphling")) || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserCommonAdded--;
                Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else if ((dota2item.Item_rarity == "uncommon") && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserUncommonAdded--;
                Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
            }
            else if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
            {
                UserRareAdded--;
                Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);
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
                    if ((dota2item.Item_rarity == "common" || dota2item.Item_rarity ==null )&& dota2item.Prefab == "wearable")
                    {

                        if (Trade.AddItemByDefindex(item.Defindex))
                        {
                            BotCommonAdded++;
                            Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);

                        }
                        else
                        {
                            Trade.SendMessage("我没有 " + msg);
                        }
                    }
                    else
                    {
                        Trade.SendMessage("这个机器人只支持交换普通装备");
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
                        BotCommonAdded--;
                        Trade.SendMessage("机器人添加:" + "普通 " + BotCommonAdded + " 用户添加:" + "普通 " + UserCommonAdded + "罕见 " + UserUncommonAdded + " 稀有 " + UserRareAdded);

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
                    Trade.SendMessage("你添加的普通必须大于等于机器人添加的普通的" + CommonExangeRate + "倍");
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

            if (IsAdmin || ((BotCommonAdded * Commonvalue* CommonExangeRate ) <= (UserCommonAdded * Commonvalue + UserUncommonAdded * Uncommonvalue + UserRareAdded * Rarevalue)))
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

