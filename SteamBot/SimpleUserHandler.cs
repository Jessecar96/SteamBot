using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;

namespace SteamBot
{
    public class SimpleUserHandler : UserHandler
    {
        int botCardAdded, userCardAdded,isthisacard= 0;
       // string[] playerCardName = new string[85] { " Akke ", " Loda ", " AdmiralBulldog ", " EGM ", " S4 ", " universe ", " sneyking ", " Aui_2000 ", " Waytosexy ", " fogged ", " BurNIng ", " Super ", " rOtk ", " QQQ ", " X!! ", " Fly ", " N0tail ", " Era ", " H4nn1 ", " Trixi ", " ChuaN ", " Zhou ", " Ferrari_430 ", " YYF ", " Faith ", " xiao8 ", " DDC ", " Yao ", " Sylar ", " DD ", " Misery ", " Pajkatt ", " God ", " 1437 ", " Brax ", " ixmike88 ", " FLUFFNSTUFF ", " TC ", " Bulba ", " Korok ", " Black^ ", " syndereN ", " FATA ", " paS ", " qojqva ", " Winter ", " FzFz ", " TFG ", " Ling ", " dabeliuteef ", " Dendi ", " XBOCT ", " Puppey ", " Funn1k ", " KuroKy ", " Mushi ", " Xtinct ", " ohayo ", " ky.xy ", " net ", " 7ckngmad ", " Funzii ", " Sockshka ", " Silent ", " Goblak ", " Kabu ", " Lanm ", " Sag ", " Icy ", " Luo ", " Hao ", " Mu ", " Sansheng ", " KingJ ", " Banana ", " ARS-ART ", " NS ", " KSi ", " Crazy ", " Illidan ", " iceiceice ", " xFreedom ", " xy ", " Yamateh ", " Ice " };
        static string[] playerCardName = new string[85] { " akke ", " loda ", " admiralbulldog ", " egm ", " s4 ", " universe ", " sneyking ", " aui_2000 ", " waytosexy ", " fogged ", " burning ", " super ", " rotk ", " qqq ", " x!! ", " fly ", " n0tail ", " era ", " h4nn1 ", " trixi ", " chuan ", " zhou ", " ferrari_430 ", " yyf ", " faith ", " xiao8 ", " ddc ", " yao ", " sylar ", " dd ", " misery ", " pajkatt ", " god ", " 1437 ", " brax ", " ixmike88 ", " fluffnstuff ", " tc ", " bulba ", " korok ", " black^ ", " synderen ", " fata ", " pas ", " qojqva ", " winter ", " fzfz ", " tfg ", " ling ", " dabeliuteef ", " dendi ", " xboct ", " puppey ", " funn1k ", " kuroky ", " mushi ", " xtinct ", " ohayo ", " ky.xy ", " net ", " 7ckngmad ", " funzii ", " sockshka ", " silent ", " goblak ", " kabu ", " lanm ", " sag ", " icy ", " luo ", " hao ", " mu ", " sansheng ", " kingj ", " banana ", " ars-art ", " ns ", " ksi ", " crazy ", " illidan ", " iceiceice ", " xfreedom ", " xy ", " yamateh ", " ice " };
        static int[] playerCardDefindex = new int[85] { 10217, 10218, 10263, 10264, 10265, 10231, 10272, 10273, 10274, 10275, 10234, 10235, 10236, 10237, 10238, 10266, 10267, 10268, 10269, 10270, 10196, 10207, 10208, 10209, 10210, 10246, 10247, 10248, 10249, 10250, 10239, 10240, 10241, 10242, 10282, 10205, 10219, 10220, 10221, 10271, 10244, 10245, 10288, 10289, 10290, 10243, 10283, 10284, 10285, 10286, 10197, 10222, 10223, 10224, 10225, 10215, 10216, 10260, 10261, 10262, 10252, 10253, 10254, 10255, 10256, 10257, 10258, 10291, 10292, 10293, 10211, 10212, 10213, 10214, 10259, 10233, 10276, 10277, 10278, 10279, 10226, 10227, 10228, 10229, 10251 };
        static int[] playerCardInventory=new int[85];
        static bool[] playerCardOk = new bool[85];
        static int totalPlayCardInventory = 0;
        //static int TimerInterval = 170000;
        //static int InviteTimerInterval = 2000;
        public SimpleUserHandler (Bot bot, SteamID sid) : base(bot, sid) 
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
            botCardAdded = 0;
            userCardAdded = 0;
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
            isthisacard = 0;
            for (int i = 0; i <= 84; i++) //做一个85次的循环检验物品是否属于卡片
            {
                if (item.Defindex == playerCardDefindex[i])
                {
                    //userCardAdded++; //如果是卡片，Bot添加物品成功用户添加卡片记录加1
                    if (i == 1 || i == 50 || i == 51 || i == 53 || i == 26 || i == 27 || i == 28)
                    {
                        userCardAdded = userCardAdded + 3;
                    }
                    else if (i < 5 || i == 52 || i == 54 || (19 < i && i < 25))
                    {
                        userCardAdded = userCardAdded + 2;
                    }
                    else
                    {

                        if (playerCardInventory[i] * 20 > totalPlayCardInventory)
                        {
                            Trade.SendMessage("因为这种卡片已超过卡片总数的5%,我现在不接受这种卡片 ");
                        }
                        else
                        {

                            userCardAdded++;
                        }
                    }
                }
                else
                {
                    isthisacard++;
                }
            }
            if (isthisacard == 85)
            {
                Trade.SendMessage("You added a item which is not a player card , and if you'd like to donate something,i appreciate it. ");//不是卡片则提示用户，不做其他操作
            }
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            for (int i = 0; i <= 84; i++) //做一个85次的循环检验物品是否属于卡片
            {
                if (item.Defindex == playerCardDefindex[i])
                {
                    //userCardAdded--; //如果是卡片，用户添加卡片记录-1
                    if (i == 1 || i == 50 || i == 51 || i == 53 || i == 26 || i == 27 || i == 28)
                    {
                        userCardAdded = userCardAdded - 3;
                    }
                    else if (i < 5 || i == 52 || i == 54 || (19 < i && i < 25) || (15 < i && i < 19))
                    {
                        userCardAdded = userCardAdded - 2;
                    }
                    else
                    {
                        if (playerCardInventory[i] * 20 > totalPlayCardInventory)
                        {
                            Trade.SendMessage("你移除了我不接受的卡片 ");
                        }
                        else
                        {
                            userCardAdded--;
                        }
                    }
                }
                else
                {

                    isthisacard++; 
                }
            }
            if (isthisacard == 85)
            {
                Trade.SendMessage("You remove a item which is not a player card.  ");//不是卡片则提示用户，不做其他操作
            }
        }
        
         public override void OnTradeMessage(string message) //根据用户在交易窗口的指令添加及移除卡
        {
            Bot.log.Info("[TRADE MESSAGE] " + message);
            message = message.ToLower();
            isthisacard = 0; 
            if ( message.Contains("add") )
            {
                for (int i = 0; i <= 84; i++)
                {
                    if ( message.Contains(playerCardName[i]) )
                  
                    {
                        if (Trade.AddItemByDefindex(playerCardDefindex[i]))
                  
                        
                        {
                            //botCardAdded++;
                            if (i == 1 || i == 50 || i == 51 || i == 53 || i == 26 || i == 27 || i == 28)
                            {
                                botCardAdded = botCardAdded + 3;
                            }
                            else if (i < 5 || i == 52 || i == 54 || (19 < i && i < 25) || (15 < i && i < 19))
                            {
                                botCardAdded = botCardAdded + 2;
                            }
                            else
                            {
                                botCardAdded++;
                            }
                        }
                        else
                        {
                            isthisacard++;
                            Trade.SendMessage("机器人没有" + playerCardName[i] + "的卡片");
                        }
                    }
                }
                if (isthisacard == 85)
                {
                    Trade.SendMessage("请输入正确的卡片名字");
                }
             }
            else if ( message.Contains("remove") )
            {
                for (int i = 0; i <= 84; i++)
                {
                    if ( message.Contains(playerCardName[i]) )
                    if (Trade.RemoveItemByDefindex(playerCardDefindex[i]))
                    {
                        //botCardAdded--;
                        if (i == 1 || i == 50 || i == 51 || i == 53 || i == 26 || i == 27 || i == 28)
                        {
                            botCardAdded = botCardAdded - 3;
                        }
                        else if (i < 5 || i == 52 || i == 54 || (19 < i && i < 25) || (15 < i && i < 19))
                        {
                            botCardAdded = botCardAdded - 2;
                        }
                        else
                        {
                            botCardAdded--;
                        }
                    }
                    else
                    {
                        isthisacard++;
                     }
                 }
                if (isthisacard == 85)
                {
                    Trade.SendMessage("请输入正确的卡片名字");
                }
            }
            else
            {
                Trade.SendMessage("Please use add xxx yyy or remove xxx yyy to add and remove cards");
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
            base.OnTradeClose();
        }

        public bool Validate ()
        {

            if (IsAdmin || (userCardAdded > 0 && botCardAdded < userCardAdded))
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

