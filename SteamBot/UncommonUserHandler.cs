using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class UncommonUserHandler : UserHandler
    {
        int BotUncommonAdded , UserUncommonAdded=0;
        bool acard = false;
        //string[] playerCardName = new string[85] { " Akke ", " Loda ", " AdmiralBulldog ", " EGM ", " S4 ", " universe ", " sneyking ", " Aui_2000 ", " Waytosexy ", " fogged ", " BurNIng ", " Super ", " rOtk ", " QQQ ", " X!! ", " Fly ", " N0tail ", " Era ", " H4nn1 ", " Trixi ", " ChuaN ", " Zhou ", " Ferrari_430 ", " YYF ", " Faith ", " xiao8 ", " DDC ", " Yao ", " Sylar ", " DD ", " Misery ", " Pajkatt ", " God ", " 1437 ", " Brax ", " ixmike88 ", " FLUFFNSTUFF ", " TC ", " Bulba ", " Korok ", " Black^ ", " syndereN ", " FATA ", " paS ", " qojqva ", " Winter ", " FzFz ", " TFG ", " Ling ", " dabeliuteef ", " Dendi ", " XBOCT ", " Puppey ", " Funn1k ", " KuroKy ", " Mushi ", " Xtinct ", " ohayo ", " ky.xy ", " net ", " 7ckngmad ", " Funzii ", " Sockshka ", " Silent ", " Goblak ", " Kabu ", " Lanm ", " Sag ", " Icy ", " Luo ", " Hao ", " Mu ", " Sansheng ", " KingJ ", " Banana ", " ARS-ART ", " NS ", " KSi ", " Crazy ", " Illidan ", " iceiceice ", " xFreedom ", " xy ", " Yamateh ", " Ice " };
        static string[] playerCardName = new string[85] { " akke ", " loda ", " admiralbulldog ", " egm ", " s4 ", " universe ", " sneyking ", " aui_2000 ", " waytosexy ", " fogged ", " burning ", " super ", " rotk ", " qqq ", " x!! ", " fly ", " n0tail ", " era ", " h4nn1 ", " trixi ", " chuan ", " zhou ", " ferrari_430 ", " yyf ", " faith ", " xiao8 ", " ddc ", " yao ", " sylar ", " dd ", " misery ", " pajkatt ", " god ", " 1437 ", " brax ", " ixmike88 ", " fluffnstuff ", " tc ", " bulba ", " korok ", " black^ ", " synderen ", " fata ", " pas ", " qojqva ", " winter ", " fzfz ", " tfg ", " ling ", " dabeliuteef ", " dendi ", " xboct ", " puppey ", " funn1k ", " kuroky ", " mushi ", " xtinct ", " ohayo ", " ky.xy ", " net ", " 7ckngmad ", " funzii ", " sockshka ", " silent ", " goblak ", " kabu ", " lanm ", " sag ", " icy ", " luo ", " hao ", " mu ", " sansheng ", " kingj ", " banana ", " ars-art ", " ns ", " ksi ", " crazy ", " illidan ", " iceiceice ", " xfreedom ", " xy ", " yamateh ", " ice " };
        static int[] playerCardDefindex = new int[85] { 10217, 10218, 10263, 10264, 10265, 10231, 10272, 10273, 10274, 10275, 10234, 10235, 10236, 10237, 10238, 10266, 10267, 10268, 10269, 10270, 10196, 10207, 10208, 10209, 10210, 10246, 10247, 10248, 10249, 10250, 10239, 10240, 10241, 10242, 10282, 10205, 10219, 10220, 10221, 10271, 10244, 10245, 10288, 10289, 10290, 10243, 10283, 10284, 10285, 10286, 10197, 10222, 10223, 10224, 10225, 10215, 10216, 10260, 10261, 10262, 10252, 10253, 10254, 10255, 10256, 10257, 10258, 10291, 10292, 10293, 10211, 10212, 10213, 10214, 10259, 10233, 10276, 10277, 10278, 10279, 10226, 10227, 10228, 10229, 10251 };
        static int[] playerCardInventory=new int[85];
        static bool[] playerCardOk = new bool[85];
        static int totalPlayCardInventory = 0;
        int isthisacard, userCardAdded, botCardAdded ,userRareAdded = 0;
        static int TimerInterval = 170000;
        static int InviteTimerInterval = 2000;
        public UncommonUserHandler(Bot bot, SteamID sid)
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
            BotUncommonAdded = 0;
            UserUncommonAdded = 0;
            userRareAdded = 0;
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
            TradeCountInventory(true);
            Trade.SendMessage ("Success. Please put up your items.And type add xxx yyy or remove xxx yyy to ask bot to add or remove player card");
        }
        
        public bool  TradeCountInventory(bool ok)
        {
            totalPlayCardInventory = 0;
            for (int ii = 0; ii < 85; ii++)
            {

                playerCardInventory[ii] = 0;

            }
            // Let's count our inventory
            Inventory.Item[] inventory = Trade.MyInventory.Items;
            
            foreach (Inventory.Item item in inventory)
            {
               
                for (int ii = 0; ii < 85; ii++)
                {
                    if (item.Defindex == playerCardDefindex[ii])
                    {
                        playerCardInventory[ii]++;
                    }
                }
            }
            for (int ii = 0; ii < 85; ii++)
            {

                totalPlayCardInventory = totalPlayCardInventory + playerCardInventory[ii] ;

            }
            return true;
            
        }
        
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);

            if (dota2item.Item_rarity == "uncommon" && dota2item.Prefab == "wearable")
            {
                UserUncommonAdded++;
                Trade.SendMessage("机器人添加:" + "罕见 " + BotUncommonAdded + " 用户添加:" + "罕见 " + UserUncommonAdded + " 稀有 " + userRareAdded);
            }
            else
            {
                Trade.SendMessage("你移除了一件我不支持的物品 ");//不是卡片则提示用户，不做其他操作   
            }
            
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            
            var item = Trade.CurrentSchemazh.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);

                if (dota2item.Item_rarity == "uncommon" && dota2item.Prefab =="wearable")
                {
                    UserUncommonAdded --;
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
                if (dota2item.Item_rarity == "uncommon")
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

            else if (message.Contains("remove"))
            {
                msg = msg.Remove(0, 6);
                msg = msg.Trim();
                var item = Trade.CurrentSchemazh.GetItemByZhname(msg);
               
                
                if (Trade.RemoveItemByDefindex(item.Defindex))
                {
                    BotUncommonAdded--;
                }
                else
                {
                    Trade.SendMessage("机器人没有添加 " + msg);
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
                    Trade.SendMessage("你添加的卡片必须大于机器人添加的卡片");
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
            Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate ()
        {

            if (IsAdmin || ((userCardAdded > 0 && botCardAdded < (userCardAdded  + userRareAdded * 5))) || (userCardAdded == 0 && botCardAdded <= (userRareAdded * 5)))
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

