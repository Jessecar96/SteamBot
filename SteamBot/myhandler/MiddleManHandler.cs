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
        int    UserRareAdded  , BotRareAdded, BotKeyAdded , UserKeyAdded = 0;

        bool middleitemadd = false;
        bool credititemadd = false;
        MiddleManItem.Record record = new MiddleManItem.Record();
        List<ulong> CreditItemAdded = new List<ulong>();
        int UserItemAdded = 0;
        int RareWardNum, UncommonWardNum  = 0;
        int fakeitem = 0;
        //UserItem.Useritem item = null;
        UserItem.Useritem item = new UserItem.Useritem();
        List<UserItem.Useritem> UserItemToAdded = new List<UserItem.Useritem>();
        List <int> IndexOfItems = new List <int>();
        List <ulong> ItemIdAdded = new List <ulong>();
        int index = 0;
        int PriceKey, PriceRr = 0;
        bool SetingPrice = false;
        int TradeType = 0; // 1Ϊ���ҷ�����Ʒ,2Ϊ���Ԥ������;
        bool TradeError = false;
        static int RateOfKeyAndRare = 5;
        public static MiddleManItem  currentmiddlerecords = null;
        public static UserItem currentuseritem = null;
        static long filetime;
        static bool Warding = false;
        public MiddleManHandler(Bot bot, SteamID sid)
            : base(bot, sid) 
        {
        }
        public override void OnTradeSuccess()
        {
            if (TradeType == 1)
            {
                currentmiddlerecords.Records.Add(record );
            }
        }
        public override bool OnFriendAdd () 
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") added me!");
            // Using a timer here because the message will fail to send if you do it too quickly
            
            return true;
        }
        public void ReInit()
        {
          
          
           TradeError = false;
           TradeType = 0;
           middleitemadd = false;
           credititemadd = false;
           
           
           
            
             
        }
        public void ReInititem()
        {
            item.Id = 0;
            item.Defindex = 0;
            item.Pricekey = 0;
            item.Pricerr = 0;
            item.Status = 0;
            item.Error = false;
            item.Item_name = "";
            item.Steam64id = OtherSID.ConvertToUInt64();
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
            if (message.Contains("confirm"))
            {
                string msg = message;
                msg = msg.Remove(0, 7);
                msg = msg.Trim();
                bool find = false;
                foreach (var xxx in currentmiddlerecords.Records )
                {
                    if (xxx.Recordid == msg && xxx.Sellersteam64id == OtherSID.ConvertToUInt64 ())
                    {
                        find = true;
                        xxx.Status = 2;// 2Ϊ����Ѿ�ȷ��
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "���� "+msg +" ����Ʒ�Ѿ�������ȷ���յ���");
                        SteamID xid = new SteamID();
                        xid.SetFromUInt64(xxx.Buyersteam64id );
                        Bot.SteamFriends.SendChatMessage(xid, type, "���� " + msg + " ����Ʒ�Ѿ�������ȷ���յ���");
                        break;
                    }
                }
                if (find == false)
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "���� " + msg + " ����Ʒû���ҵ�");
                }
                
            }
            else if (message.Contains("myitems"))
            {
                string msg = message;
                
                foreach (var xxx in currentuseritem.Items)
                {
                    if (xxx.Steam64id == OtherSID.ConvertToUInt64() )
                    {
                        string xstatus, xerror;
                        if (xxx.Status ==0)
                        {
                            xstatus = "���ڳ�����";
                        }
                        else if (xxx.Status ==1)
                        {
                            xstatus = "�ѳ���";
                        }
                        else if (xxx.Status == 2)
                        {
                            xstatus = "Ǯ��ȡ";
                        }
                        else
                        {
                            xstatus = "��Ʒ�ѱ�����������";
                        }

                        if (xxx.Error ==false)
                        {
                            xerror = "";
                        }
                        
                        else
                        {
                            xerror = "����Ʒ״̬����,�������Ա��ϵ";
                        }

                        string xmsg = "��Ʒ����:" +xxx.Item_name + " Id:" +xxx.Id + " �۸�:" +xxx.Pricekey + "key" +xxx.Pricerr + "RR  " +xstatus + "  "+xerror ;
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, xmsg);
                    }
                }
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
            if (Warding == true)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg , "���������ڲ���,���Ժ�����");
                return false;
            }
            else
            {
                Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
                UInt64  xxid = OtherSID.ConvertToUInt64();
                Bot.log.Warn( xxid.ToString()  );
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
            Trade.SendMessage("��ʼ���ɹ�.");
        }

        public  int AddRare(int num)
        {
            
           // var items = new List<Inventory.Item>();
            int i = 0;
            foreach (Inventory.Item item in Trade.MyInventory.Items)
            {

                bool incurrentitem = false;
                if (i >= num)
                {

                    break;
                }

                else
                {
                    foreach (var xxx in currentuseritem.Items)
                    {
                        if (xxx.Id == item.OriginalId)
                        {
                            incurrentitem = true;
                            break;
                        }
                    }
                    if (incurrentitem == false)
                    {
                        var dota2item = Trade.Dota2Schema.GetItem(item.Defindex);

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
            return i;
            
        }
        public int AddKey(int num)
        {

            int x = 0;
            for (int i = 0; i < num; i++)
            {
                if (Trade.AddItemByDefindex(15003))
                {
                    x++;
                }
                else
                {
                    break;
                }
            }
            return x;

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
             Trade.SendMessage("��Ӻ���" + i + "��");

        }



        public void AddCommon(int num)
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

                        if (dota2item != null && (dota2item.Item_rarity == "common" || dota2item.Item_rarity == null) && (dota2item.Prefab == "default_item" || dota2item.Prefab == "wearable"))
                        {
                            i++;
                            Trade.AddItem(item.Id);
                        }
                    }
                }
            }
             Trade.SendMessage("�����ͨ" + i + "��");
        }
          
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
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
                        middleitemadd = true;
                        record = new MiddleManItem.Record();
                        var zhitem = Trade.CurrentSchemazh.GetItem(inventoryItem.Defindex);
                        record.Sellersteam64id = OtherSID.ConvertToUInt64();
                        //ReInititem();
                        //SetingPrice = true;
                        DateTime dt = DateTime.Now;
                        string a = dt.Month.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        record.Recordid = dt.Year.ToString() + a;
                        a = dt.Day.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        record.Recordid = record.Recordid + a;
                        a = dt.Hour.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        record.Recordid = record.Recordid + a;
                        a = dt.Minute.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        record.Recordid = record.Recordid + a;
                        a = dt.Second.ToString();
                        if (a.Length < 2)
                        {
                            a = "0" + a;
                        }
                        record.Recordid = record.Recordid + a;
                        
                        Trade.SendMessage( record.Recordid  );
                        record.Id = inventoryItem.OriginalId;
                        record.Defindex = inventoryItem.Defindex;
                        record.Item_name = zhitem.ItemName;
                        Trade.SendMessage("���м��˽��׵���Ʒ�Ѽ�¼,�����һ��������ϡ����ΪѺ��,�����Ҫ������ͬ������Ѻ��,Ѻ��������õ���Ʒ�󽫱��˻�");
                    }
                    else
                    {
                        if (!credititemadd)
                        {
                            CreditItemAdded = new List<ulong>();
                        }
                        credititemadd = true;
                        var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
                        if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable"  || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
                        {
                            //UserRareAdded++;
                           // Trade.SendMessage("���������:" + "ϡ�� " + BotRareAdded + " �û����:" + "ϡ�� " + UserRareAdded);
                            CreditItemAdded.Add(inventoryItem.OriginalId );
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
                if (!credititemadd)
                {
                    CreditItemAdded = new List<ulong>();
                }
                credititemadd = true;
                var dota2item = Trade.Dota2Schema.GetItem(schemaItem.Defindex);
                if (dota2item.Item_rarity == "rare" && (dota2item.Prefab == "wearable" || dota2item.Prefab == "ward" || dota2item.Prefab == "hud_skin"))
                {
                    //UserRareAdded++;
                    // Trade.SendMessage("���������:" + "ϡ�� " + BotRareAdded + " �û����:" + "ϡ�� " + UserRareAdded);
                    CreditItemAdded.Add(inventoryItem.OriginalId);
                    record.Buyercredititems = CreditItemAdded;
                }
                else
                {
                    Trade.SendMessage("���ǲ���,���׽�ȡ��");
                    Trade.CancelTrade();
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "���ǲ���,����ȡ������򿪿����ʾ������");
                } 
            }
           

            else
            {
                Trade.SendMessage("��ǰģʽΪ������Ʒ�����������Ʒ");
            }

            
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
           
                Trade.CancelTrade();

        }
        
         public override void OnTradeMessage(string message) 
        {
           Bot.log.Info("[TRADE MESSAGE] " + message);
           string msg = message;
           if (msg.Contains("buy"))
           {
               if (TradeType == 0)
               {
                   TradeType = 2;
                   Trade.SendMessage("���趨ΪԤ������ģʽ");
               }
               if (TradeType == 2)
               {
                   msg = msg.Remove(0, 3);
                   msg = msg.Trim();
                   bool find = false;
                   foreach (var xxx in currentmiddlerecords.Records)
                   {
                       if (xxx.Recordid == msg)
                       {
                           Trade.AddItemByOriginal_id(xxx.Id );
                           foreach (var yyy in xxx.Sellercredititems)
                           {
                               Trade.AddItemByOriginal_id(yyy);
                           }
                           find = true;
                           Trade.SendMessage("��������ӵĵ�һ�������ҵĽ�����Ʒ,����ΪѺ��,����ϸȷ��");
                       }
                       
                   }
                   if (find == false)
                   {
                       Trade.SendMessage("û���ҵ���Ʒ");
                   }
                  
               }
               else
               {
                   Trade.SendMessage("��ǰ��������ģʽ������Ԥ�����������½���");
               }
           }
           else if (msg.Contains("getmoney"))
           {
               if (TradeType == 0)
               {
                   TradeType = 4;
                   Trade.SendMessage("����ģʽ�Ѿ��趨ΪȡǮģʽ");
               }
               if (TradeType == 4)
               {
                   {
                       foreach (var xxx in currentuseritem.Items)
                       {
                           if (xxx.Steam64id == OtherSID.ConvertToUInt64() && xxx.Status == 1)
                           {
                               if (xxx.Error == false)
                               {
                                   item = new UserItem.Useritem();
                                   item = xxx;
                                   //IndexOfItems.Add(currentuseritem.Items.IndexOf(xxx));
                                 //  item.Status = 2;
                                   UserItemToAdded.Add(item);
                                   Trade.SendMessage(item.Item_name + " " + "id:" + item.Id + " ���������۸��� " + item.Pricekey + "key " + item.Pricerr + "RR");
                                   BotKeyAdded = BotKeyAdded + item.Pricekey;
                                   int keyadded = AddKey(item.Pricekey);
                                   if (keyadded < item.Pricekey)
                                   {
                                       Trade.SendMessage("������key��������֪ͨ����Ա");
                                   }
                                   BotRareAdded = BotRareAdded + item.Pricerr;
                                   int rradded = AddRare(item.Pricerr);
                                   if (rradded < item.Pricerr)
                                   {
                                       Trade.SendMessage("������RR��������֪ͨ����Ա");
                                   }
                                   Trade.SendMessage("�����������:" + "key " + BotKeyAdded + " RR " + BotRareAdded + " |�û����:" + "key " + UserKeyAdded + " RR " + UserRareAdded);
                               }
                               else
                               {

                                   Trade.SendMessage("��Ʒ���ƣ�" + xxx.Item_name  + " ��Ʒoriginal_id = " + xxx.Id + " ��״̬�����޷�������ϵ����Ա");
                               
                               }
                           }
                       }
                   }
               }
               else
               {
                   Trade.SendMessage("��ǰ��������ģʽ������ȡǮ�������½���");
               }

 
           }
           else if (msg.Contains("additem"))
           {
               if (TradeType == 0)
               {
                   TradeType = 1;
                   Trade.SendMessage("����ģʽ�Ѿ��趨Ϊ����ģʽ");
               }
               if (TradeType == 1)
               {
                   msg = msg.Remove(0, 7);
                   msg = msg.Trim();
                   UserItem.Useritem yyy = new UserItem.Useritem();
                   int pricexxx = int.MaxValue;
                   foreach (var xxx in currentuseritem.Items)
                   {
                       if (xxx.Item_name == msg && !ItemIdAdded.Contains (xxx.Id ) &&xxx.Id!=0&& xxx.Error ==false&& xxx.Status == 0 && (xxx.Pricekey * RateOfKeyAndRare + xxx.Pricerr) < pricexxx)
                       {
                           yyy = xxx;
                           
                       }
                   }

                   if ( yyy==null||yyy.Id  == 0 )
                   {
                       Trade.SendMessage("û���ҵ� " + msg);
                   }
                   else
                   {
                       Trade.SendMessage("��Ʒ����:" + yyy.Item_name + " id��" + yyy.Id + " " + yyy.Pricekey + "key " + yyy.Pricerr + "RR");
                       //maybetocheck
                       Trade.AddItemByOriginal_id(yyy.Id);
                     //  IndexOfItems.Add(currentuseritem.Items.IndexOf(yyy));
                      // yyy.Status = 1;
                       UserItemToAdded.Add(yyy);
                       BotKeyAdded = BotKeyAdded + yyy.Pricekey;
                       BotRareAdded = BotRareAdded + yyy.Pricerr;
                       Trade.SendMessage("����Ҫ֧����:" + "key " + BotKeyAdded + " RR " + BotRareAdded + " |�û����:" + "key " + UserKeyAdded + " RR " + UserRareAdded);
                   }



               }

           }
           else if (msg.Contains("addbyid"))
           {
               if (TradeType == 0)
               {
                   TradeType = 1;
                   Trade.SendMessage("����ģʽ�Ѿ��趨Ϊ����ģʽ");
               }
               if (TradeType == 1)
               {
                   msg = msg.Remove(0, 7);
                   msg = msg.Trim();
                   ulong aid = Convert.ToUInt64(msg);
                   UserItem.Useritem yyy = new UserItem.Useritem();
                   int pricexxx = int.MaxValue;
                   
                   if (!ItemIdAdded.Contains(aid))
                   {

                       foreach (var xxx in currentuseritem.Items)
                       {
                           if (xxx.Id == aid && xxx.Status == 0 && (xxx.Pricekey * RateOfKeyAndRare + xxx.Pricerr) < pricexxx)
                           {
                               yyy = xxx;

                           }
                       }



                       if (yyy == null || yyy.Id == 0)
                       {
                           Trade.SendMessage("û���ҵ� " + msg);
                       }
                       else
                       {
                           if (yyy.Error != true)
                           {
                               Trade.SendMessage("��Ʒ����:" + yyy.Item_name + " id��" + yyy.Id + " " + yyy.Pricekey + "key " + yyy.Pricerr + "RR");
                               ItemIdAdded.Add(yyy.Id);
                               Trade.AddItemByOriginal_id(yyy.Id);
                               IndexOfItems.Add(currentuseritem.Items.IndexOf(yyy));
                               yyy.Status = 1;
                               UserItemToAdded.Add(yyy);
                               BotKeyAdded = BotKeyAdded + yyy.Pricekey;
                               BotRareAdded = BotRareAdded + yyy.Pricerr;
                               Trade.SendMessage("����Ҫ֧����:" + "key " + BotKeyAdded + " RR " + BotRareAdded + " |�û����:" + "key " + UserKeyAdded + " RR " + UserRareAdded);
                           }
                           else
                           {
                               Trade.SendMessage("��Ʒ original_id = " + yyy.Id + " ��״̬�����޷���ӣ�����ϵ����Ա");
                           }

                         }
                   }
                   else
                   {
                       Trade.SendMessage("Id=" + msg +"����Ʒ����ӣ��벻Ҫ�ظ�����");
                   }



               }
           }
           else if (TradeType == 2)
           {
               if (msg.Contains("key"))
               {
                   if (SetingPrice == true)
                   {

                       msg = msg.Remove(0, 3);
                       msg = msg.Trim();
                       PriceKey = Convert.ToInt32(msg);
                       Trade.SendMessage(PriceKey + "key " + PriceRr + "RR");
                   }
                   else
                   {
                       Trade.SendMessage("������µ���Ʒ���ü۸�");
                   }
               }
               else if (msg.Contains("rr"))
               {
                   if (SetingPrice == true)
                   {
                       msg = msg.Remove(0, 2);
                       msg = msg.Trim();
                       PriceRr = Convert.ToInt32(msg);
                       Trade.SendMessage(PriceKey + "key " + PriceRr + "RR");
                   }
                   else
                   {
                       Trade.SendMessage("������µ���Ʒ���ü۸�");
                   }
               }
               else if (msg.Contains("save"))
               {
                   if (SetingPrice == true)
                   {
                   item.Pricekey = PriceKey;
                   item.Pricerr = PriceRr;
                   Trade.SendMessage("��Ʒ���ƣ�" + item.Item_name + " Id:" + item.Id + " �۸�:" + PriceKey + "key " + PriceRr + "RR" + " �ѱ���");

                  // UserItem.Useritem xitem = new UserItem.Useritem() ;
                 //  xitem = item;
                   UserItemToAdded.Add(item);

                   SetingPrice = false;
                   }
                   else
                   {
                       Trade.SendMessage("������µ���Ʒ���ü۸�");
                   }
               }
               else
               {
                   Trade.SendMessage("�����ָ��");
               }
           }
           else
           {
               Trade.SendMessage("�����ָ�����δ�趨����ģʽ");
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
                    Trade.SendMessage("���ṩ�����Ҳ�֧�ֵ���Ʒ");
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
                var otheritems = Trade.OtherOfferedItems;
                var myitems = Trade.steamMyOfferedItems;
                Warding = true;
                try
                {
                    Trade.AcceptTrade();
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
                    TradeError = true;
                    //׼�������
                }
                Log.Success("Trade Complete!");
                OnTradeClose();
                 bool checkresult = false;
                if (TradeError == true)
                {
                    Inventory newInventory = Inventory.FetchInventory(Bot.SteamUser.SteamID.ConvertToUInt64(), Bot.apiKey);
                   Inventory.Item checkitem;
                   if (TradeType == 4)
                   {
                       checkitem = newInventory.GetItem(myitems[0]);
                   }
                   else
                   {
                       checkitem = newInventory.GetItemsByOriginal_id(UserItemToAdded[0].Id);
                   }
                    if (checkitem != null)
                    {
                        checkresult = true;
                    }

                    else
                    {
                        checkresult = false;
                    }

                }
                if (TradeType == 2)
                {
                    if (TradeError == true)
                    {
                        if (checkresult == true)
                        {
                            TradeError = false;
                        }
                    }

                    Additemstofile();
                }
                else if (TradeType == 1)
                {
                    if (TradeError == true)
                    {
                        if (checkresult == false)
                        {
                            TradeError = false;
                        }
                    }
                    Sellitemsfromfile();
                }
                else if (TradeType == 3)
                {
                    if (TradeError == true)
                    {
                        if (checkresult == false)
                        {
                            TradeError = false;
                        }
                    }
                    ToRemoveitemsfromfile();
                }
                else if (TradeType == 4)
                {
                    if (TradeError == true)
                    {
                        if (checkresult == false)
                        {
                            TradeError = false;
                        }
                    }
                    Moneygiveditemsfromfile();
                }
                else
                {
                    Bot.log.Warn("Tradetype is 0");
                }
                Warding = false;

                
                
            }
            else
            {
                Trade.SetReady(false);
            }
            
            
        }

        public void Writejson()
        {
            
            string json = JsonConvert.SerializeObject(currentuseritem);
            string path= @"useritem.json";
            string jsonadd = JsonConvert.SerializeObject(UserItemToAdded );
            Bot.log.Warn(jsonadd);
            StreamWriter sw = new StreamWriter(path, false);
            sw.WriteLine(json);
            sw.Close();
            // д���ļ���

        }

        public  void Additemstofile()
        {
            if (TradeError == true)
            {
                for (int i = 0; i < UserItemToAdded.Count; i++)
                {
                    UserItemToAdded[i].Error = true;
                }
            }



            foreach (var xxx in UserItemToAdded)
                {
                    currentuseritem.Items.Add(xxx);
                    
                }
            Writejson();
            
            // д���ļ���

        }
        public void Sellitemsfromfile()
        {
            /*foreach (var xxx in UserItemToAdded)
            {
                int x = 0;
                    foreach (var yyy in currentuseritem.Items )
                    {
                        if (yyy.Id ==xxx.Id && yyy.Steam64id ==xxx.Steam64id && yyy.Defindex ==xxx.Defindex  &&yyy.Status== 0)
                        {
                            x = currentuseritem.Items.IndexOf(yyy);
                            currentuseritem.Items[x].Status = 1;
                        }
                    }
            } */ 
            /*
            foreach (var xxx in IndexOfItems )
            {
                currentuseritem.Items[xxx].Status = 1;
                if (TradeError == true)
                {
                    currentuseritem.Items[xxx].Error = true;
                }
            }  */
            foreach (var xxx in UserItemToAdded )
            {
                xxx.Status = 1;
                if (TradeError == true)
                {
                    xxx.Error = true;
                }
            }
            Writejson();// д���ļ���

        }

        public void ToRemoveitemsfromfile()
        {   /*
            foreach (var xxx in UserItemToAdded)
            {
                int x = 0;
                foreach (var yyy in currentuseritem.Items)
                {
                    if (yyy.Id == xxx.Id && yyy.Steam64id == xxx.Steam64id && yyy.Defindex == xxx.Defindex &&   yyy.Status==0 )
                    {
                        x = currentuseritem.Items.IndexOf(yyy);
                        currentuseritem.Items[x].Status = 3;
                    }
                }
            } */
            /*
            foreach (var xxx in IndexOfItems)
            {
                currentuseritem.Items[xxx].Status = 3;
                if (TradeError == true)
                {
                    currentuseritem.Items[xxx].Error = true;
                }
            } */
            foreach (var xxx in UserItemToAdded)
            {
                xxx.Status = 3;
                if (TradeError == true)
                {
                    xxx.Error = true;
                }
            }
            Writejson(); // д���ļ���

        }

        public void Moneygiveditemsfromfile()
        { /*
            foreach (var xxx in UserItemToAdded)
            {
                int x = 0;
                foreach (var yyy in currentuseritem.Items)
                {
                    if (yyy.Id == xxx.Id && yyy.Steam64id == xxx.Steam64id && yyy.Defindex == xxx.Defindex && yyy.Status == 1)
                    {
                        x = currentuseritem.Items.IndexOf(yyy);
                        currentuseritem.Items[x].Status = 2;
                    }
                }
            } */ /*
            foreach (var xxx in IndexOfItems)
            {
                currentuseritem.Items[xxx].Status = 2;
                if (TradeError == true)
                {
                    currentuseritem.Items[xxx].Error = true;
                }
            } */

            foreach (var xxx in UserItemToAdded)
            {
                xxx.Status = 2;
                if (TradeError == true)
                {
                    xxx.Error = true;
                }
            }
            Writejson(); // д���ļ���

        }


        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            //Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate ()
        {

            if (TradeType == 2 || TradeType == 3)
            {
                return true;
            }
            else if (TradeType == 1 /*|| TradeType == 4 */)
            {
               if (BotKeyAdded <= UserKeyAdded && BotRareAdded <= UserKeyAdded)
                {
                    return true;
               }
                else
               {
                    Trade.SendMessage("����Ҫ֧����:" + "key " + BotKeyAdded + " RR " + BotRareAdded+" |�û����:" + "key " + UserKeyAdded + " RR " + UserRareAdded);
                  Trade.SendMessage("����ӵ�key������ڵ�����֧����key������ӵ�RR������ڵ�����֧����RR");
                    return false;
                }
            }
            else if (TradeType == 4)
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

