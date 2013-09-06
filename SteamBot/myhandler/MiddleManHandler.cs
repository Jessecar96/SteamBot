using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;
using Newtonsoft.Json;
using System.IO;

namespace SteamBot
{
    public class MiddleManHandler : UserHandler
    {
        int UserRareAdded, BotRareAdded, BotKeyAdded, UserKeyAdded = 0;

        bool middleitemadd = false;
        bool credititemadd = false;
        MiddleManItem.Record record = new MiddleManItem.Record();
        List<ulong> CreditItemAdded = new List<ulong>();
        MiddleManItem.Record recordtomodify = new MiddleManItem.Record();
        //UserItem.Useritem item = null;
        //UserItem.Useritem item = new UserItem.Useritem();
      //  List<UserItem.Useritem> UserItemToAdded = new List<UserItem.Useritem>();
      //  List<int> IndexOfItems = new List<int>();
       // List<ulong> ItemIdAdded = new List<ulong>();
        int TradeType = 0; // 1为卖家放置物品,2为买家预定购买,3为买家拿货,4为卖家拿押金
        bool TradeError = false;
        public static MiddleManItem currentmiddlerecords = null;
       // public static UserItem currentuseritem = null;
        static long filetime;
        static bool Warding = false;
        static bool successlock, adminlock = false;
        public MiddleManHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
        }
        public override void OnTradeSuccess()
        {
            
            successlock = true;
            if (TradeType == 1)//卖家添加物品
            {
                currentmiddlerecords.Records.Add(record);
                Writejson();
            }
            else if (TradeType == 2)//买家放押金
            {
                recordtomodify.Status = 1;
                recordtomodify.Buyercredititems = CreditItemAdded;
                recordtomodify.Buyersteam64id = OtherSID.ConvertToUInt64();
                SteamID buyerid = new SteamID();
                buyerid.SetFromUInt64(recordtomodify.Buyersteam64id);
                Bot.SteamFriends.SendChatMessage(buyerid, EChatEntryType.ChatMsg , "单号 " + recordtomodify.Recordid  + " 的物品已经由 "+recordtomodify.Buyersteam64id+ " 买家付押金预定");
                Writejson();
            }
            else if (TradeType == 3)//买家拿物品
            {
                recordtomodify.Status = 3;//status 由 2卖家已确认变为3买家拿走
                SteamID buyerid = new SteamID();
                buyerid.SetFromUInt64(recordtomodify.Sellersteam64id );
                Bot.SteamFriends.SendChatMessage(buyerid, EChatEntryType.ChatMsg, "单号 " + recordtomodify.Recordid + " 的物品已经由64位id为 " + recordtomodify.Buyersteam64id + " 的买家拿走,你可以拿走你的押金了");
                Writejson();

            }
            else if (TradeType == 4)
            {
                recordtomodify.Status = 4;//卖家拿走押金
                Writejson();
            }
            else if (TradeType == 5)
            {
                recordtomodify.Status = 5;//卖家取消中间人交易,有0变为5
                Writejson();
            }
            else
            {
            }
            successlock = false;
        }
        public override bool OnFriendAdd()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ConvertToUInt64() + ") added me!");
            // Using a timer here because the message will fail to send if you do it too quickly

            return true;
        }
        public void ReInit()
        {
            TradeError = false;
            TradeType = 0;
            middleitemadd = false;
            credititemadd = false;
            UserRareAdded = 0;
            BotRareAdded = 0;
        }
        public void ReInititem()
        {

        }


        public override void OnLoginCompleted()
        {
            if (currentmiddlerecords == null)
            {
                currentmiddlerecords = MiddleManItem.FetchSchema();
                Bot.log.Success("middlerecords 读取完成");
            }

        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override void OnFriendRemove()
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") removed me!");
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            // if (message == ".removeall")
            //{
            // Commenting this out because RemoveAllFriends is a custom function I wrote.
            // Bot.SteamFriends.RemoveAllFriends();
            // Bot.log.Warn("Removed all friends from my friends list.");
            // }
            if (!adminlock)
            {
                if (message.Contains("confirm"))
                {

                    string msg = message;
                    msg = msg.Remove(0, 7);
                    msg = msg.Trim();
                    bool find = false;
                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg && xxx.Sellersteam64id == OtherSID.ConvertToUInt64())
                        {
                            find = true;
                            xxx.Status = 2;// 2为买家已经确认
                            Bot.SteamFriends.SendChatMessage(OtherSID, type, "单号 " + msg + " 的物品已经由卖家确认收到款");
                            SteamID xid = new SteamID();
                            xid.SetFromUInt64(xxx.Buyersteam64id);
                            Bot.SteamFriends.SendChatMessage(xid, type, "单号 " + msg + " 的物品已经由卖家确认收到款");
                            if (!successlock)
                            {
                                Writejson();
                            }
                            break;
                        }
                    }
                    if (find == false)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "单号 " + msg + " 的物品没有找到");
                    }

                }
                else if (message.Contains("info"))
                {
                    string msg = message;
                    msg = msg.Remove(0, 4);
                    msg = msg.Trim();
                    bool find = false;
                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg)
                        //    && xxx.Sellersteam64id == OtherSID.ConvertToUInt64())
                        {
                            find = true;

                            Bot.SteamFriends.SendChatMessage(OtherSID, type, "单号 " + msg + "  " + xxx.Sellersteam64id);

                            break;
                        }
                    }
                    if (find == false)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "单号 " + msg + " 的物品没有找到");
                    }

                }

                else
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "错误的命令");
                }
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "管理员锁定机器人维护中,请稍等");
            }
            if (message.Contains("stop") && IsAdmin)
            {
                if (successlock)
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "有人在操作");
                }
                else
                {
                    adminlock = true;
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "保存文件中");
                    Writejson();
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "文件保存成功");

                }
            }
            else if (message.Contains("start") && IsAdmin)
            {
                currentmiddlerecords = MiddleManItem.FetchSchema();
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "middlerecords 读取完成");
                adminlock = false;
            }
            else
            { 
            }
            //Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
        }

        public override bool OnTradeRequest()
        { /*
            long timecheck = DateTime.Now.ToFileTime();
            if ((timecheck - filetime )>350000000)
            {
                Warding=false;
            } */
            if (successlock||adminlock )
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "其他人正在操作,请稍后再试");
                return false;
            }
            else
            {
                Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
                UInt64 xxid = OtherSID.ConvertToUInt64();
                Bot.log.Warn(xxid.ToString()+" 邀请机器人交易成功");
                return true;
            }
        }

        public override void OnTradeError(string error)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID,
                                              EChatEntryType.ChatMsg,
                                              "Oh, there was an error: " + error + "."
                                              );
            Bot.log.Warn(error);
        }

        public override void OnTradeTimeout()
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info("User was kicked because he was AFK.");
        }

        public override void OnTradeInit()
        {
            ReInit();
            //TradeCountInventory(true);
            Trade.SendMessage("初始化成功.");
        }

        

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            if (TradeType == 0)
            {
                TradeType = 1;
            }
            if (TradeType == 1)
            {
                if (inventoryItem == null)
                {

                    Trade.SendMessage("无法识别物品，请打开仓库显示,交易即将取消");
                    Trade.CancelTrade();
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "无法识别物品，请打开仓库显示,交易取消，请打开库存显示后重试");

                }
                else
                {
                    if (!middleitemadd)
                    {
                        middleitemadd = true;//中间人物品已添加设置为true
                        record = new MiddleManItem.Record();//创建新的rencord
                       // var zhitem = Trade.CurrentSchemazh.GetItem(inventoryItem.Defindex);
                        record.Sellersteam64id = OtherSID.ConvertToUInt64();//获得卖家64bit ID
                        record.Recordid=gettimestring ();//获得201309060708类型的时间的字符串
                        Trade.SendMessage(record.Recordid);
                        record.Id = inventoryItem.OriginalId;
                        record.Defindex = inventoryItem.Defindex;
                       // record.Item_name = zhitem.ItemName;
                        Trade.SendMessage("需中间人交易的物品已记录,请放置一定数量的稀有作为押金,买家需要缴纳相同数量的押金,押金在买家拿到物品后将被退回");
                    }
                    else
                    {
                        if (!credititemadd)
                        {
                            CreditItemAdded = new List<ulong>();//如果还没有添加押金创建个新的list
                        }
                        credititemadd = true;
                        var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
                        if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
                        {
                            UserRareAdded++;
                            Trade.SendMessage(" 用户添加押金:" + "稀有 " + UserRareAdded);
                            CreditItemAdded.Add(inventoryItem.OriginalId);
                            record.Sellercredititems = CreditItemAdded;
                        }
                        else
                        {
                            Trade.SendMessage("不是菠菜,交易将取消");
                            Trade.CancelTrade();
                            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                                      "不是菠菜,交易取消，请打开库存显示后重试");
                        }

                    }


                }
            }
            else if (TradeType == 2)
            {
                Trade.RemoveAllItems();
                if (!credititemadd)
                {
                    CreditItemAdded = new List<ulong>();
                    credititemadd = true;
                }
                var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
                if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
                {
                    UserRareAdded++;
                    Trade.SendMessage("所需:" + "稀有 " + BotRareAdded + " 用户添加:" + "稀有 " + UserRareAdded);
                    CreditItemAdded.Add(inventoryItem.OriginalId);
                    //record.Buyercredititems = CreditItemAdded;
                }
                else
                {
                    Trade.SendMessage("不是菠菜,交易将取消");
                    Trade.CancelTrade();
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "不是菠菜,交易取消，请重试");
                }
            }


            else
            {
                Trade.SendMessage("当前模式为返还物品，无需添加物品");
            }


        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {

            Trade.CancelTrade();
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "任何情况下移除物品将取消交易");

        }
        
        public override void OnTradeMessage(string message)
        {
            Bot.log.Info("[TRADE MESSAGE] " + message);
            string msg = message;
            if (msg.Contains("buyitem"))
            {
                if (TradeType == 0)
                {
                    TradeType = 2;
                    Trade.SendMessage("已设定为预定购买模式");
                //if (TradeType == 2)
               // {
                    msg = msg.Remove(0, 7);
                    msg = msg.Trim();
                    bool find = false;
                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg)
                        {
                            recordtomodify = xxx;
                            find = true;
                            Trade.AddItemByOriginal_id(xxx.Id);
                            if (xxx.Sellercredititems != null)
                            {
                                foreach (var yyy in xxx.Sellercredititems)
                                {
                                    Trade.AddItemByOriginal_id(yyy);
                                }
                            }
                            
                            Trade.SendMessage("机器人添加的第一个是卖家的交易物品,其余为押金,请仔细确认");
                        }

                    }
                    if (find == false)
                    {
                        Trade.SendMessage("没有找到物品");
                        TradeType=0;
                        Trade.SendMessage("模式已初始化");
                    }

                //}
                }

                else
                {
                    Trade.SendMessage("当前处于其他模式，不能预定购买，请重新交易");
                }
            }
            else if (msg.Contains("buyerget"))
            {
                if (TradeType == 0)
                {
                    TradeType = 3; //买家拿到物品及拿回押金
                    Trade.SendMessage("交易模式已经设定为买家拿取物品模式");
                 //if (TradeType == 3)
                //{
                    msg = msg.Remove(0, 8);
                    msg = msg.Trim();
                    bool find = false;
                    
                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg && xxx.Buyersteam64id ==OtherSID.ConvertToUInt64() && xxx.Status ==2)
                        {
                            recordtomodify = xxx;
                            find = true;
                            Trade.AddItemByOriginal_id(xxx.Id );
                            foreach (var yyy in xxx.Buyercredititems)
                            {
                                Trade.AddItemByOriginal_id (yyy);
                            }
                            break;
                        }
                    }
                    if (find)
                    {
                        Trade.SendMessage("物品已放置");
                    }
                    else
                    {
                        Trade.SendMessage("没有找到符合要求的物品");
                        TradeType=0;
                        Trade.SendMessage("模式已初始化");

                    }

               // }
                }

                else
                {
                    Trade.SendMessage("错误的命令");
                }


            }
            else if (msg.Contains("sellerget"))
            {
                if (TradeType == 0)
                {
                    TradeType = 4; //卖家拿回押金
                    Trade.SendMessage("交易模式已经设定为卖家拿回押金模式");
                //if (TradeType == 4)
               // {
                    msg = msg.Remove(0, 9);
                    msg = msg.Trim();
                    bool find = false;

                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg && xxx.Sellersteam64id  == OtherSID.ConvertToUInt64() && xxx.Status == 3)//3为买家已拿回物品
                        {
                            recordtomodify = xxx;
                            find = true;
                            //Trade.AddItemByOriginal_id(xxx.Id);
                            foreach (var yyy in xxx.Sellercredititems )
                            {
                                Trade.AddItemByOriginal_id(yyy);
                            }
                            break;
                        }
                    }
                    if (find)
                    {
                        Trade.SendMessage("物品已放置");
                    }
                    else
                    {
                        Trade.SendMessage("没有找到符合要求的物品");
                        TradeType=0;
                        Trade.SendMessage("模式已初始化");
                    }

                   }
                    else
                   {
                       Trade.SendMessage("请不要重复输入指令");
                    }
                }

                 else if (msg.Contains("cancel"))
                {
                if (TradeType == 0)
                {
                    TradeType = 5; //卖家取消
                    Trade.SendMessage("交易模式已经设定为卖家取消中间人模式");
                //if (TradeType == 5)
               // {
                    msg = msg.Remove(0, 6);
                    msg = msg.Trim();
                    bool find = false;

                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg && xxx.Sellersteam64id  == OtherSID.ConvertToUInt64() && xxx.Status == 0)//0为还没有买家预定的物品
                        {
                            recordtomodify = xxx;
                            find = true;
                            Trade.AddItemByOriginal_id(xxx.Id);
                            foreach (var yyy in xxx.Sellercredititems )
                            {
                                Trade.AddItemByOriginal_id(yyy);
                            }
                            break;
                        }
                    }
                    if (find)
                    {
                        Trade.SendMessage("物品已放置");
                    }
                    else
                    {
                        Trade.SendMessage("没有找到符合要求的物品");
                        TradeType=0;
                        Trade.SendMessage("模式已初始化");
                    }

              //  }
                }

                else
                {
                    Trade.SendMessage("请不要重复输入指令");
                }

            }

            else
            {
                Trade.SendMessage("错误的指令或者未设定交易模式");
            }

        }

        public override void OnTradeReady(bool ready)
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
                   // Trade.SendMessage("你提供的有我不支持的物品");
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


        }

        public void Writejson()
        {

            string json = JsonConvert.SerializeObject(currentmiddlerecords);
            string path = @"middlemanitem.json";
            //string jsonadd = JsonConvert.SerializeObject(UserItemToAdded );
            // Bot.log.Warn(jsonadd);
            StreamWriter sw = new StreamWriter(path, false);
            sw.WriteLine(json);
            sw.Close();
            // 写入文件；

        }

        

        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            //Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate()
        {
            if (TradeType == 1)//卖家放物品及押金
            {
                if (UserRareAdded > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (TradeType == 2)//买家放押金
            {
                /*
                if (Trade.steamMyOfferedItems.Count > 0)
                {
                    Trade.RemoveAllItems();
                    return false;
                }
                Bot.log.Warn("2"); */
                if (BotRareAdded <= UserRareAdded && UserRareAdded >0)
                {
                    return true;
                }
                else
                {
                    Trade.SendMessage("你添加的稀有数量与卖家添加的不符");
                    return false;
                }
            }
            else if (TradeType == 3 || TradeType == 4 || TradeType == 5)//3为买家拿,4为卖家拿,5为卖家取消
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public string gettimestring()
        {
                        DateTime dt = DateTime.Now;
                        string a = dt.Month.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        string b = dt.Year.ToString() + a;
                        a = dt.Day.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        b = b + a;
                        a = dt.Hour.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        b = b + a;
                        a = dt.Minute.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        b = b + a;
                        a = dt.Second.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        b = b + a;
            return b;
        }

    }    
    
 
}

