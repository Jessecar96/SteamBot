using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;
using System.Threading;
using System.Linq;
using System.Text;

namespace SteamBot
{
    public class Bot1UserHandler : UserHandler
    {
        int botCardAdded, userCardAdded, isthisacard, userRareAdded = 0;
        bool acard = false;
       // string[] playerCardName = new string[85] { " Akke ", " Loda ", " AdmiralBulldog ", " EGM ", " S4 ", " universe ", " sneyking ", " Aui_2000 ", " Waytosexy ", " fogged ", " BurNIng ", " Super ", " rOtk ", " QQQ ", " X!! ", " Fly ", " N0tail ", " Era ", " H4nn1 ", " Trixi ", " ChuaN ", " Zhou ", " Ferrari_430 ", " YYF ", " Faith ", " xiao8 ", " DDC ", " Yao ", " Sylar ", " DD ", " Misery ", " Pajkatt ", " God ", " 1437 ", " Brax ", " ixmike88 ", " FLUFFNSTUFF ", " TC ", " Bulba ", " Korok ", " Black^ ", " syndereN ", " FATA ", " paS ", " qojqva ", " Winter ", " FzFz ", " TFG ", " Ling ", " dabeliuteef ", " Dendi ", " XBOCT ", " Puppey ", " Funn1k ", " KuroKy ", " Mushi ", " Xtinct ", " ohayo ", " ky.xy ", " net ", " 7ckngmad ", " Funzii ", " Sockshka ", " Silent ", " Goblak ", " Kabu ", " Lanm ", " Sag ", " Icy ", " Luo ", " Hao ", " Mu ", " Sansheng ", " KingJ ", " Banana ", " ARS-ART ", " NS ", " KSi ", " Crazy ", " Illidan ", " iceiceice ", " xFreedom ", " xy ", " Yamateh ", " Ice " };
        string[] playerCardName = new string[85] { " akke ", " loda ", " admiralbulldog ", " egm ", " s4 ", " universe ", " sneyking ", " aui_2000 ", " waytosexy ", " fogged ", " burning ", " super ", " rotk ", " qqq ", " x!! ", " fly ", " n0tail ", " era ", " h4nn1 ", " trixi ", " chuan ", " zhou ", " ferrari_430 ", " yyf ", " faith ", " xiao8 ", " ddc ", " yao ", " sylar ", " dd ", " misery ", " pajkatt ", " god ", " 1437 ", " brax ", " ixmike88 ", " fluffnstuff ", " tc ", " bulba ", " korok ", " black^ ", " synderen ", " fata ", " pas ", " qojqva ", " winter ", " fzfz ", " tfg ", " ling ", " dabeliuteef ", " dendi ", " xboct ", " puppey ", " funn1k ", " kuroky ", " mushi ", " xtinct ", " ohayo ", " ky.xy ", " net ", " 7ckngmad ", " funzii ", " sockshka ", " silent ", " goblak ", " kabu ", " lanm ", " sag ", " icy ", " luo ", " hao ", " mu ", " sansheng ", " kingj ", " banana ", " ars-art ", " ns ", " ksi ", " crazy ", " illidan ", " iceiceice ", " xfreedom ", " xy ", " yamateh ", " ice " };
        static int[] playerCardDefindex = new int[85] { 10217, 10218, 10263, 10264, 10265, 10231, 10272, 10273, 10274, 10275, 10234, 10235, 10236, 10237, 10238, 10266, 10267, 10268, 10269, 10270, 10196, 10207, 10208, 10209, 10210, 10246, 10247, 10248, 10249, 10250, 10239, 10240, 10241, 10242, 10282, 10205, 10219, 10220, 10221, 10271, 10244, 10245, 10288, 10289, 10290, 10243, 10283, 10284, 10285, 10286, 10197, 10222, 10223, 10224, 10225, 10215, 10216, 10260, 10261, 10262, 10252, 10253, 10254, 10255, 10256, 10257, 10258, 10291, 10292, 10293, 10211, 10212, 10213, 10214, 10259, 10233, 10276, 10277, 10278, 10279, 10226, 10227, 10228, 10229, 10251 };
        static int[] playerCardInventory = new int[85];
        static bool[] playerCardOk = new bool[85];
        static int totalPlayCardInventory = 0;
        int postionInQueue = 0;
        long filetime;
        public Bot1UserHandler(Bot bot, SteamID sid): base(bot, sid) 
        {
        }

        public void leavethequeue(SteamID OtherSid, EChatEntryType type)
        {
            if (Bot1Queue.locked == false)
            {
                if (status(OtherSid, type))
                {
                    Bot1Queue.locked = true;
                    Bot1Queue.requesttradefiletime = DateTime.Now.ToFileTime();
                    for (int i = postionInQueue - 1; i < Bot1Queue.peopleInQueue; i++)
                    {
                        Bot1Queue.steamidInQueue[i] = Bot1Queue.steamidInQueue[i + 1];
                    }
                    if (Bot1Queue.peopleInQueue > 1)
                    {
                        Bot1Queue.peopleInQueue--;
                    }
                    else
                    {
                        Bot1Queue.peopleInQueue = 0;
                    }
                    Bot1Queue.locked = false;
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "你已经离开了队伍 ");
                }
                else
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "你不在队伍中 ");
                }
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "机器人忙,稍候再试 ");
            }
        }

        public bool status(SteamID OtherSid, EChatEntryType type)
        {
            retrade ();
            bool inqueue = false;
            for (int i = 0; i < Bot1Queue.peopleInQueue; i++)
            {
                if (Bot1Queue.steamidInQueue[i] == OtherSid)
                {
                    postionInQueue = i + 1;
                    inqueue = true;
                    break;
                }
            }
            if (inqueue == true)
            {
                
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "你排在 " + postionInQueue + "/" + Bot1Queue.peopleInQueue + "的位置");
                return true;
            }
            else
            {

                Bot.SteamFriends.SendChatMessage(OtherSID, type, "你不在队伍中,队伍现在有" + Bot1Queue.peopleInQueue + "个人");
                return false;

            }
        }
        public void jointhequeue(SteamID OtherSid, EChatEntryType type)
        {
            /*if (Bot1Queue.peopleInQueue == 0)
            {
                if (Bot.CurrentTrade == null)
                {
                    if (Bot.OpenTrade(OtherSid))
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "我已经邀请你");
                    }
                    else
                    {
                        Bot1Queue.steamidInQueue[Bot1Queue.peopleInQueue] = OtherSid;
                        Bot1Queue.peopleInQueue++;
                        postionInQueue = Bot1Queue.peopleInQueue;
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "you are the" + postionInQueue + "/" + Bot1Queue.peopleInQueue + "in the queue ");
                        Bot.OpenTrade(Bot1Queue.steamidInQueue[0]);
                    }

                }
                else
                {
                    Bot1Queue.steamidInQueue[Bot1Queue.peopleInQueue] = OtherSid;
                    Bot1Queue.peopleInQueue++;
                    postionInQueue = Bot1Queue.peopleInQueue;
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "you are the" + postionInQueue + "/" + Bot1Queue.peopleInQueue + "in the queue ");
                    Bot.OpenTrade(Bot1Queue.steamidInQueue[0]);
                }
            }
            else
            {

                Bot1Queue.steamidInQueue[Bot1Queue.peopleInQueue] = OtherSid;
                Bot1Queue.peopleInQueue++;
                postionInQueue = Bot1Queue.peopleInQueue;
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "you are the" + postionInQueue + "/" + Bot1Queue.peopleInQueue + " in the queue ");
            }*/
            bool inqueue = false;
            for (int i = 0; i < Bot1Queue.peopleInQueue; i++)
            {
                if (Bot1Queue.steamidInQueue[i] == OtherSid)
                {
                    postionInQueue = i + 1;
                    inqueue = true;
                    break;
                }
            }
            if (inqueue ==true )
            {

                Bot.SteamFriends.SendChatMessage(OtherSID, type, "你排在 " + postionInQueue + "/" + Bot1Queue.peopleInQueue + "的位置,无法重新加入");
                
            }
            else
            {
                Bot1Queue.steamidInQueue[Bot1Queue.peopleInQueue] = OtherSid;
                Bot1Queue.peopleInQueue++;
                postionInQueue = Bot1Queue.peopleInQueue;
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "you are the" + postionInQueue + "/" + Bot1Queue.peopleInQueue + " in the queue ");
            }
            
            if (Bot1Queue.locked==false && Bot.CurrentTrade ==null )
            {
                retrade();
            }

        }

        public bool HandleCallback()
        {
            Bot.SteamClient.WaitForCallback();
            CallbackMsg msg = Bot.SteamClient.GetCallback();
            //Log.Warn(Bot.SteamClient.GetCallback().GetType().ToString());
            if (Bot.SteamClient.GetCallback().GetType().ToString() == "SteamKit2.SteamTrading+TradeResultCallback")
            {
                return false;
            }
            else
            {
                Bot.SteamClient.FreeLastCallback();
                return true;
            }
        }

        public bool tradenext()
        {

            //if (Bot1Queue.locked == false && Bot.CurrentTrade == null && Bot1Queue.peopleInQueue != 0)
            //{
                //Thread.Sleep(10000);
                //Bot1Queue.requestTrade = true;
                //if (Bot.CurrentTrade == null && Bot1Queue.peopleInQueue != 0)
            long dttime =  DateTime.Now.ToFileTime();
            if (Bot.CurrentTrade == null && Bot1Queue.peopleInQueue != 0 && (dttime - Bot1Queue.requesttradefiletime) > 500000000)
            {

                Bot1Queue.locked = true;
                Bot1Queue.success = 0;
                //DateTime dt =DateTime.Now;

                Bot1Queue.requesttradefiletime = DateTime.Now.ToFileTime();
                Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
                Bot.OpenTrade(Bot1Queue.steamidInQueue[0]);
               
                /*
                while (HandleCallback())
                {
                    HandleCallback();
                }
                CallbackMsg msg = Bot.SteamClient.GetCallback();
                msg.Handle<SteamTrading.TradeResultCallback>(callback =>
                {
                    //Log.Warn("Trade Status: " + callback.Response);

                    if (callback.Response == EEconTradeResponse.Accepted)
                    {
                        Bot1Queue.success = 0;
                        Bot.SteamClient.FreeLastCallback();
                        Log.Success("Trade Accepted");
                        Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
                    }
                    if (callback.Response == EEconTradeResponse.InitiatorAlreadyTrading)
                    {
                        Bot1Queue.success = 1;
                        Bot.SteamClient.FreeLastCallback();
                        Log.Warn("Bot already trading");
                    }

                    if (callback.Response == EEconTradeResponse.TradeBannedTarget)
                    {
                        Bot1Queue.success = 2;
                        Bot.SteamClient.FreeLastCallback();
                        Log.Error("Trade Target Banned");
                    }
                    if (callback.Response == EEconTradeResponse.Cancel ||
                        callback.Response == EEconTradeResponse.ConnectionFailed ||
                        callback.Response == EEconTradeResponse.Declined ||
                        callback.Response == EEconTradeResponse.Error ||
                        callback.Response == EEconTradeResponse.TargetAlreadyTrading ||
                        callback.Response == EEconTradeResponse.Timeout ||
                        callback.Response == EEconTradeResponse.TooSoon ||
                        callback.Response == EEconTradeResponse.TradeBannedInitiator ||
                        callback.Response == EEconTradeResponse.TradeBannedTarget ||
                        callback.Response == EEconTradeResponse.NotLoggedIn) // uh...
                    {
                        Bot1Queue.success = 2;
                        Bot.SteamClient.FreeLastCallback();
                        Log.Error("Trade Errored");
                        //db.RemovefromQue(que[0]);
                        //List<string> playerque = db.checkQueforPlayer(botID, que[2]);

                        //Player still exists in que, don't remove from friendlist
                        if (playerque.Count() > 0)
                        {
                            Bot.SteamFriends.SendChatMessage(player, EChatEntryType.ChatMsg, "Trade timed out, however you are still in que for another transaction so I will keep you as my friend");
                        }
                        //Player no longer in que table, get rid of them
                        else
                        {
                            Bot.SteamFriends.SendChatMessage(player, EChatEntryType.ChatMsg, "Trade timed out you must re-add me and enter tradeID again");
                            if (!IsAdmin)
                            {
                                Bot.SteamFriends.RemoveFriend(player);
                            }
                        }
                    } 
                }); */



                if (Bot1Queue.peopleInQueue > 1)
                {
                    Bot.SteamFriends.SendChatMessage(Bot1Queue.steamidInQueue[1], EChatEntryType.ChatMsg, "这个交易完成之后,我将交易你,请做好准备 ");
                }

                if (Bot1Queue.peopleInQueue > 1)
                {
                    for (int i = 0; i < Bot1Queue.peopleInQueue; i++)
                    {
                        Bot1Queue.steamidInQueue[i] = Bot1Queue.steamidInQueue[i + 1];
                    }
                    Bot1Queue.peopleInQueue--;
                }
                else
                {
                    Bot1Queue.peopleInQueue = 0;
                }


                //Thread.Sleep(60000);
                Bot1Queue.locked = false;
                //  if (Bot1Queue.success == 2)
                //{
                //   return false;
                // }
                // else

                return false;
            }
            //}

               // }
            //}
            else
            {
                return true; //如果当前在交易或者在交易邀请,返回真,不继续邀请交易
            }   
            
        }
        public void retrade()
        {
            filetime  = DateTime.Now.ToFileTime();
            if ((filetime - Bot1Queue.requesttradefiletime) > 600000000)
            {
                Bot1Queue.locked = false;
            }
            do { }
           while (!tradenext());
            //tradenext();
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
            message = message.ToLower();
            retrade();
            if (message.Contains("/join"))
            {
                jointhequeue(OtherSID, type);
            }
            else if (message.Contains("/status"))
            {
                status(OtherSID, type);
                 
            }
           // else if (message.Contains("/leave"))
            //{
            //    leavethequeue(OtherSID, type);
           // }
            else
            {

                Bot.SteamFriends.SendChatMessage(OtherSID,
                                                  EChatEntryType.ChatMsg,
                                                  "我现在启用了排队系统,请使用 /join 加入队伍 使用 /status 查询你的排队状态 使用 /leave 离开队伍 "
                                                  );
            }
        }


        public override bool OnTradeRequest() 
        {
            
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,"我现在启用了排队系统,请使用 /join 加入队伍 使用 /status 查询你的排队状态 使用 /leave 离开队伍 " );
            return false;       
        }
        
        public override void OnTradeError (string error) 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, 
                                              EChatEntryType.ChatMsg,
                                              "Oh, there was an error: " + error + "."
                                              );
            
            Bot.log.Warn (error);
            retrade();
        }
        
        public override void OnTradeTimeout () 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were AFK and the trade was canceled.");
            
            Bot.log.Info ("User was kicked because he was AFK.");
            retrade();
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

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            // var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
            /*if (Trade.Dota2Schema == null)
            {
                Trade.SendMessage("null ");
            } */
            isthisacard = 0;
            acard = false;
            for (int i = 0; i <= 84; i++) //做一个85次的循环检验物品是否属于卡片
            {
                if (item.Defindex == playerCardDefindex[i])
                {
                    acard = true;
                    //userCardAdded++; //如果是卡片，Bot添加物品成功用户添加卡片记录加1
                    if (i == 1 || i == 50 || i == 51 || i == 53 || i == 26 || i == 27 || i == 28)
                    {
                        userCardAdded = userCardAdded + 4;
                        Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                        break;
                    }
                    else if (i < 5 || i == 52 || i == 54 || (19 < i && i < 25))
                    {
                        userCardAdded = userCardAdded + 2;
                        Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                        break;
                    }
                    else
                    {

                        if (playerCardInventory[i] * 20 > totalPlayCardInventory)
                        {
                            Trade.SendMessage("因为这种卡片已超过卡片总数的5%,我现在不接受这种卡片 ");

                            break;
                        }
                        else
                        {

                            userCardAdded++;
                            Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                            break;
                        }
                    }
                }

            }
            if (acard == false)
            {
                var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
                if (dota2item.Item_rarity == "rare" && !(dota2item.Name.Contains("Taunt")) && !(dota2item.Name.Contains("Treasure")))
                {
                    userRareAdded++;
                    Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                }
                else
                {
                    Trade.SendMessage("你添加了一件即不是卡片,又不是菠菜的物品 ");//不是卡片则提示用户，不做其他操作   
                }
                //Trade.SendMessage("You added a item which is not a player card , and if you'd like to donate something,i appreciate it. ");//不是卡片则提示用户，不做其他操作
            }
            //Trade.SendMessage(  Trade.Dota2Schema.Items.Length.ToString );

        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            acard = false;
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            for (int i = 0; i <= 84; i++) //做一个85次的循环检验物品是否属于卡片
            {
                
                if (item.Defindex == playerCardDefindex[i])
                {
                    acard = true;
                    //userCardAdded--; //如果是卡片，用户添加卡片记录-1
                    if (i == 1 || i == 50 || i == 51 || i == 53 || i == 26 || i == 27 || i == 28)
                    {
                        userCardAdded = userCardAdded - 4;
                        Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                        break;
                    }
                    else if (i < 5 || i == 52 || i == 54 || (19 < i && i < 25) || (15 < i && i < 19))
                    {
                        userCardAdded = userCardAdded - 2;
                        Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                        break;
                    }
                    else
                    {
                        if (playerCardInventory[i] * 20 > totalPlayCardInventory)
                        {
                            Trade.SendMessage("你移除了我不接受的卡片 ");
                            break;
                        }
                        else
                        {
                            userCardAdded--;
                            Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                            break;
                        }
                    }
                }

            }
            if (acard == false)
            {
                var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);

                if (dota2item.Item_rarity == "rare" && !(dota2item.Name.Contains("Taunt")) && !(dota2item.Name.Contains("Treasure")) && dota2item.Defindex != 10066)
                {
                    userRareAdded--;
                    Trade.SendMessage("机器人添加:" + "卡片 " + botCardAdded + " 用户添加:" + "卡片 " + userCardAdded + " 稀有 " + userRareAdded);
                }
                else
                {
                    Trade.SendMessage("你移除了一件即不是卡片,又不是菠菜的物品 ");//不是卡片则提示用户，不做其他操作   
                }

                 
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
                                botCardAdded = botCardAdded + 4;
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
                            botCardAdded = botCardAdded - 4;
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
            Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            Bot.CloseTrade();
            retrade();
            
        }

        public bool Validate ()
        {

            if (IsAdmin || ((userCardAdded > 0 && botCardAdded < userCardAdded + userRareAdded * 5)) || (userCardAdded == 0 && botCardAdded <= userRareAdded * 5))
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

