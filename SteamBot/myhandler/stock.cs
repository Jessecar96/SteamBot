using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net.Mail;
namespace SteamBot
{
    public class StockHandler : UserHandler
    {

        static bool StockSuccess = false;
        static int SleepTime = 60000;
        public StockHandler(Bot bot, SteamID sid)
            : base(bot, sid) 
        {
        }
        public override void OnTradeSuccess()
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

        }
        private void StartStockThread()
        {
            // initialize data to use in thread


            var pollThread = new Thread(() =>
            {
                StockSuccess = false;
                const string UrlBase = "http://store.valvesoftware.com/index.php?t=2&g=10";
                string url = UrlBase;
                int i = 0;
                string result;
                while (!StockSuccess)
                {
                    i++;
                    HttpWebRequest request = null;
                    HttpWebResponse response = null;
                    StreamReader reader = null;
                    try
                    {
                        result = "";
                        request = (HttpWebRequest)HttpWebRequest.Create(url);
                        request.Method = "GET";
                        request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                        request.Host = "store.valvesoftware.com";
                        request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
                        request.Referer = "http://store.valvesoftware.com";
                        response = request.GetResponse() as HttpWebResponse;
                        reader = new StreamReader(response.GetResponseStream());
                        result = reader.ReadToEnd();
                        Regex r = new Regex("product.php");
                        int x = r.Matches(result).Count;
                        if (x != 14)
                        {
                            SteamID myid = new SteamID();
                            myid.SetFromUInt64(76561198047154762);
                            Bot.SteamFriends.SendChatMessage(myid, EChatEntryType.ChatMsg, "valve商店有新物品");
                            MailMessage mailsend = new MailMessage ();
                            mailsend.Body = "valve商店有新物品";
                            mailsend.From =new MailAddress ("me_sunyue@163.com");
                            mailsend.To.Add(new MailAddress ("812477104@qq.com"));
                            mailsend.Subject = "valve商店有货";
                            mailsend.SubjectEncoding = System.Text.Encoding.UTF8;
                            mailsend.IsBodyHtml = false;
                            mailsend.Priority = MailPriority.High;
                            //smtp client   
                            SmtpClient sender = new SmtpClient();
                            sender.Host = "smtp.163.com";
                            sender.Port = 25;
                            sender .UseDefaultCredentials　=　true;
                            sender.Credentials = new System.Net.NetworkCredential("me_sunyue@163.com","qwerty19891211");
                            sender.DeliveryMethod = SmtpDeliveryMethod.Network;
                            sender.EnableSsl = false;
                            try
                            {
                                sender.Send(mailsend );

                            }
                            catch (Exception e)
                            {
                                Log.Warn("发送邮件失败 "+ e.ToString ());
                            }   
                            StockSuccess = true;
                            Log.Warn("valve商店有新物品");
                        }
                        if (i >= 60)
                        {
                            i = 0;
                            Log.Warn(result);
                        }


                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex.ToString());


                    }
                    finally
                    {
                        if (request != null) request.Abort();
                        if (response != null) response.Close();
                        if (reader != null) reader.Close();
 
                    }
                    Thread.Sleep(SleepTime);
                }


            });

            pollThread.Start();
        }

        public override void OnLoginCompleted()
        {
            Log.Warn("启动");
            StartStockThread();
            Log.Warn("启动成功");
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {

        }

        public override void OnFriendRemove () 
        {

        }
        
        public override void OnMessage (string message, EChatEntryType type) 
        {
            string msg = message.ToLower();
            if (message.Contains("stock"))
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, StockSuccess.ToString() );
            }
            if (message.Contains("time"))
            {
                msg = msg.Trim();
                msg = msg.Remove(0, 4);
                msg = msg.Trim();
                SleepTime = Convert.ToInt32(msg);
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "时间调整为"+msg +"毫秒");
            }

        }

        public override bool OnTradeRequest() 
        {
            return true;

        }
        
        public override void OnTradeError (string error) 
        {

        }
        
        public override void OnTradeTimeout () 
        {

        }
        
        public override void OnTradeInit() 
        {
            ReInit();

        }
        
        
          
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
        }
   
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            

        }
        
         public override void OnTradeMessage(string message) //根据用户在交易窗口的指令添加及移除卡
        {

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

                return true;

        }
        
    }
 
}

