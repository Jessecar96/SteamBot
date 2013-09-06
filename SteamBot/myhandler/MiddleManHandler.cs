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
        int TradeType = 0; // 1Ϊ���ҷ�����Ʒ,2Ϊ���Ԥ������,3Ϊ����û�,4Ϊ������Ѻ��
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
            if (TradeType == 1)//���������Ʒ
            {
                currentmiddlerecords.Records.Add(record);
                Writejson();
            }
            else if (TradeType == 2)//��ҷ�Ѻ��
            {
                recordtomodify.Status = 1;
                recordtomodify.Buyercredititems = CreditItemAdded;
                recordtomodify.Buyersteam64id = OtherSID.ConvertToUInt64();
                SteamID buyerid = new SteamID();
                buyerid.SetFromUInt64(recordtomodify.Buyersteam64id);
                Bot.SteamFriends.SendChatMessage(buyerid, EChatEntryType.ChatMsg , "���� " + recordtomodify.Recordid  + " ����Ʒ�Ѿ��� "+recordtomodify.Buyersteam64id+ " ��Ҹ�Ѻ��Ԥ��");
                Writejson();
            }
            else if (TradeType == 3)//�������Ʒ
            {
                recordtomodify.Status = 3;//status �� 2������ȷ�ϱ�Ϊ3�������
                SteamID buyerid = new SteamID();
                buyerid.SetFromUInt64(recordtomodify.Sellersteam64id );
                Bot.SteamFriends.SendChatMessage(buyerid, EChatEntryType.ChatMsg, "���� " + recordtomodify.Recordid + " ����Ʒ�Ѿ���64λidΪ " + recordtomodify.Buyersteam64id + " ���������,������������Ѻ����");
                Writejson();

            }
            else if (TradeType == 4)
            {
                recordtomodify.Status = 4;//��������Ѻ��
                Writejson();
            }
            else if (TradeType == 5)
            {
                recordtomodify.Status = 5;//����ȡ���м��˽���,��0��Ϊ5
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
                Bot.log.Success("middlerecords ��ȡ���");
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
                            xxx.Status = 2;// 2Ϊ����Ѿ�ȷ��
                            Bot.SteamFriends.SendChatMessage(OtherSID, type, "���� " + msg + " ����Ʒ�Ѿ�������ȷ���յ���");
                            SteamID xid = new SteamID();
                            xid.SetFromUInt64(xxx.Buyersteam64id);
                            Bot.SteamFriends.SendChatMessage(xid, type, "���� " + msg + " ����Ʒ�Ѿ�������ȷ���յ���");
                            if (!successlock)
                            {
                                Writejson();
                            }
                            break;
                        }
                    }
                    if (find == false)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "���� " + msg + " ����Ʒû���ҵ�");
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

                            Bot.SteamFriends.SendChatMessage(OtherSID, type, "���� " + msg + "  " + xxx.Sellersteam64id);

                            break;
                        }
                    }
                    if (find == false)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "���� " + msg + " ����Ʒû���ҵ�");
                    }

                }

                else
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "���������");
                }
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "����Ա����������ά����,���Ե�");
            }
            if (message.Contains("stop") && IsAdmin)
            {
                if (successlock)
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "�����ڲ���");
                }
                else
                {
                    adminlock = true;
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "�����ļ���");
                    Writejson();
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "�ļ�����ɹ�");

                }
            }
            else if (message.Contains("start") && IsAdmin)
            {
                currentmiddlerecords = MiddleManItem.FetchSchema();
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "middlerecords ��ȡ���");
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
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "���������ڲ���,���Ժ�����");
                return false;
            }
            else
            {
                Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
                UInt64 xxid = OtherSID.ConvertToUInt64();
                Bot.log.Warn(xxid.ToString()+" ��������˽��׳ɹ�");
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
            Trade.SendMessage("��ʼ���ɹ�.");
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

                    Trade.SendMessage("�޷�ʶ����Ʒ����򿪲ֿ���ʾ,���׼���ȡ��");
                    Trade.CancelTrade();
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "�޷�ʶ����Ʒ����򿪲ֿ���ʾ,����ȡ������򿪿����ʾ������");

                }
                else
                {
                    if (!middleitemadd)
                    {
                        middleitemadd = true;//�м�����Ʒ���������Ϊtrue
                        record = new MiddleManItem.Record();//�����µ�rencord
                       // var zhitem = Trade.CurrentSchemazh.GetItem(inventoryItem.Defindex);
                        record.Sellersteam64id = OtherSID.ConvertToUInt64();//�������64bit ID
                        record.Recordid=gettimestring ();//���201309060708���͵�ʱ����ַ���
                        Trade.SendMessage(record.Recordid);
                        record.Id = inventoryItem.OriginalId;
                        record.Defindex = inventoryItem.Defindex;
                       // record.Item_name = zhitem.ItemName;
                        Trade.SendMessage("���м��˽��׵���Ʒ�Ѽ�¼,�����һ��������ϡ����ΪѺ��,�����Ҫ������ͬ������Ѻ��,Ѻ��������õ���Ʒ�󽫱��˻�");
                    }
                    else
                    {
                        if (!credititemadd)
                        {
                            CreditItemAdded = new List<ulong>();//�����û�����Ѻ�𴴽����µ�list
                        }
                        credititemadd = true;
                        var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
                        if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
                        {
                            UserRareAdded++;
                            Trade.SendMessage(" �û����Ѻ��:" + "ϡ�� " + UserRareAdded);
                            CreditItemAdded.Add(inventoryItem.OriginalId);
                            record.Sellercredititems = CreditItemAdded;
                        }
                        else
                        {
                            Trade.SendMessage("���ǲ���,���׽�ȡ��");
                            Trade.CancelTrade();
                            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                                      "���ǲ���,����ȡ������򿪿����ʾ������");
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
                    Trade.SendMessage("����:" + "ϡ�� " + BotRareAdded + " �û����:" + "ϡ�� " + UserRareAdded);
                    CreditItemAdded.Add(inventoryItem.OriginalId);
                    //record.Buyercredititems = CreditItemAdded;
                }
                else
                {
                    Trade.SendMessage("���ǲ���,���׽�ȡ��");
                    Trade.CancelTrade();
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "���ǲ���,����ȡ����������");
                }
            }


            else
            {
                Trade.SendMessage("��ǰģʽΪ������Ʒ�����������Ʒ");
            }


        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {

            Trade.CancelTrade();
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "�κ�������Ƴ���Ʒ��ȡ������");

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
                    Trade.SendMessage("���趨ΪԤ������ģʽ");
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
                            
                            Trade.SendMessage("��������ӵĵ�һ�������ҵĽ�����Ʒ,����ΪѺ��,����ϸȷ��");
                        }

                    }
                    if (find == false)
                    {
                        Trade.SendMessage("û���ҵ���Ʒ");
                        TradeType=0;
                        Trade.SendMessage("ģʽ�ѳ�ʼ��");
                    }

                //}
                }

                else
                {
                    Trade.SendMessage("��ǰ��������ģʽ������Ԥ�����������½���");
                }
            }
            else if (msg.Contains("buyerget"))
            {
                if (TradeType == 0)
                {
                    TradeType = 3; //����õ���Ʒ���û�Ѻ��
                    Trade.SendMessage("����ģʽ�Ѿ��趨Ϊ�����ȡ��Ʒģʽ");
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
                        Trade.SendMessage("��Ʒ�ѷ���");
                    }
                    else
                    {
                        Trade.SendMessage("û���ҵ�����Ҫ�����Ʒ");
                        TradeType=0;
                        Trade.SendMessage("ģʽ�ѳ�ʼ��");

                    }

               // }
                }

                else
                {
                    Trade.SendMessage("���������");
                }


            }
            else if (msg.Contains("sellerget"))
            {
                if (TradeType == 0)
                {
                    TradeType = 4; //�����û�Ѻ��
                    Trade.SendMessage("����ģʽ�Ѿ��趨Ϊ�����û�Ѻ��ģʽ");
                //if (TradeType == 4)
               // {
                    msg = msg.Remove(0, 9);
                    msg = msg.Trim();
                    bool find = false;

                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg && xxx.Sellersteam64id  == OtherSID.ConvertToUInt64() && xxx.Status == 3)//3Ϊ������û���Ʒ
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
                        Trade.SendMessage("��Ʒ�ѷ���");
                    }
                    else
                    {
                        Trade.SendMessage("û���ҵ�����Ҫ�����Ʒ");
                        TradeType=0;
                        Trade.SendMessage("ģʽ�ѳ�ʼ��");
                    }

                   }
                    else
                   {
                       Trade.SendMessage("�벻Ҫ�ظ�����ָ��");
                    }
                }

                 else if (msg.Contains("cancel"))
                {
                if (TradeType == 0)
                {
                    TradeType = 5; //����ȡ��
                    Trade.SendMessage("����ģʽ�Ѿ��趨Ϊ����ȡ���м���ģʽ");
                //if (TradeType == 5)
               // {
                    msg = msg.Remove(0, 6);
                    msg = msg.Trim();
                    bool find = false;

                    foreach (var xxx in currentmiddlerecords.Records)
                    {
                        if (xxx.Recordid == msg && xxx.Sellersteam64id  == OtherSID.ConvertToUInt64() && xxx.Status == 0)//0Ϊ��û�����Ԥ������Ʒ
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
                        Trade.SendMessage("��Ʒ�ѷ���");
                    }
                    else
                    {
                        Trade.SendMessage("û���ҵ�����Ҫ�����Ʒ");
                        TradeType=0;
                        Trade.SendMessage("ģʽ�ѳ�ʼ��");
                    }

              //  }
                }

                else
                {
                    Trade.SendMessage("�벻Ҫ�ظ�����ָ��");
                }

            }

            else
            {
                Trade.SendMessage("�����ָ�����δ�趨����ģʽ");
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
                   // Trade.SendMessage("���ṩ�����Ҳ�֧�ֵ���Ʒ");
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
            // д���ļ���

        }

        

        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            //Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate()
        {
            if (TradeType == 1)//���ҷ���Ʒ��Ѻ��
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
            else if (TradeType == 2)//��ҷ�Ѻ��
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
                    Trade.SendMessage("����ӵ�ϡ��������������ӵĲ���");
                    return false;
                }
            }
            else if (TradeType == 3 || TradeType == 4 || TradeType == 5)//3Ϊ�����,4Ϊ������,5Ϊ����ȡ��
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

