using SteamKit2;
using System.Collections.Generic;
using SteamTrade;

namespace SteamBot
{
    public class SimpleUserHandler : UserHandler
    {
        public int ScrapPutUp;
        public int botCardAdded, userCardAdded,isthisacard= 0;
        public string[] playerCardName = new string [85] {"Akke","Loda","AdmiralBulldog","EGM","S4","universe","sneyking","Aui_2000","Waytosexy","fogged","BurNIng","Super","rOtk","QQQ","X!!","Fly","N0tail","Era","H4nn1","Trixi","ChuaN","Zhou","Ferrari_430","YYF","Faith","xiao8","DDC","Yao","Sylar","DD","Misery","Pajkatt","God","1437","Brax","ixmike88","FLUFFNSTUFF","TC","Bulba","Korok","Black^","syndereN","FATA","paS","qojqva","Winter","FzFz","TFG","Ling","dabeliuteef","Dendi","XBOCT","Puppey","Funn1k","KuroKy","Mushi","Xtinct","Akke","Loda","FLUFFNSTUFF","7ckngmad","Funzii","Sockshka","Silent","Goblak","Kabu","Lanm","Sag","Icy","Luo","Hao","Mu","Sansheng","KingJ","Banana","ARS-ART","NS","KSi","Crazy","Illidan","iceiceice","xFreedom","xy","Yamateh","ice"};
        int[] playerCardDefindex = new int [85] {10217,10218,10263,10264,10265,10231,10272,10273,10274,10275,10234,10235,10236,10237,10238,10266,10267,10268,10269,10270,10196,10207,10208,10209,10210,10246,10247,10248,10249,10250,10239,10240,10241,10242,10282,10205,10219,10220,10221,10271,10244,10245,10288,10289,10290,10243,10283,10284,10285,10286,10197,10222,10223,10224,10225,10215,10216,10217,10218,10219,10252,10253,10254,10255,10256,10257,10258,10291,10292,10293,10211,10212,10213,10214,10259,10233,10276,10277,10278,10279,10226,10227,10228,10229,10251};
        public SimpleUserHandler (Bot bot, SteamID sid) : base(bot, sid) {}
        static int TimerInterval = 170000;
        static int InviteTimerInterval = 2000;
        public override bool OnFriendAdd () 
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") added me!");
            // Using a timer here because the message will fail to send if you do it too quickly
            
            return true;
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
            Trade.SendMessage ("Success. Please put up your items.And type add xxx yyy or remove xxx yyy to ask bot to add or remove player card");
        }
        
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);//获取添加物品信息并赋予变量item
            isthisacard = 0;
            for (int i = 0; i <= 84; i++) //做一个85次的循环检验物品是否属于卡片
            {
                if (item.Defindex == playerCardDefindex[i])
                {
                    userCardAdded++; //如果是卡片，Bot添加物品成功用户添加卡片记录加1
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
                    userCardAdded--; //如果是卡片，用户添加卡片记录-1
                }
                else
                {

                    isthisacard++; 
                }
            }
            if (isthisacard == 85)
            {
                Trade.SendMessage("YYou remove a item which is not a player card.  ");//不是卡片则提示用户，不做其他操作
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
                if (message.Contains(playerCardName[i]))
                    if (Trade.AddItemByDefindex(playerCardDefindex[i]))
                    {
                        botCardAdded++;
                        Trade.SendMessage("Bot added a player card. ");
                    }
                    else
                    {
                        isthisacard++;
                    }
                }
                if (isthisacard == 85)
                {
                    Trade.SendMessage("you typed the wrong card name");
                }
             }
            else if ( message.Contains("remove") )
            {
                for (int i = 0; i <= 84; i++)
                {
                    if ( message.Contains(playerCardName[i]) )
                    if (Trade.RemoveItemByDefindex(playerCardDefindex[i]))
                    {
                        botCardAdded--;
                     Trade.SendMessage("Bot removed a player card. ");
                    }
                    else
                    {
                        isthisacard++;
                     }
                 }
                if (isthisacard == 85)
                {
                    Trade.SendMessage("you typed the wrong card name");
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
        
        public override void OnTradeAccept() 
        {
           
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try {
                    Trade.AcceptTrade();
                }
                catch {
                    Log.Warn ("The trade might have failed, but we can't be sure.");
                }

                Log.Success ("Trade Complete!");
           

            OnTradeClose ();
        }

        public bool Validate ()
        {

            if (userCardAdded > 0 && botCardAdded < userCardAdded)
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

