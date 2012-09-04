
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using SteamKit2;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using System.Collections;
using SteamKit2.GC;
using SteamKit2.Internal;

namespace SteamBot
{
    public class Bot : ICertificatePolicy
    {

        //SteamRE Variables
        public SteamFriends steamFriends;
        public SteamClient steamClient;
        public SteamTrading steamTrade;
        public SteamGameCoordinator steamCoordinator;
        public SteamUser steamUser;

        //Trading Variables
        public CookieCollection WebCookies;
        public TradeSystem trade;

        public bool isLoggedIn = false;

        //Other Variables
        public string[] AllArgs;

        public string sessionId;
        public string token;

        public ulong[] Admins
        {
            get { return theBot.Admins; }
        }

        public BotInfo theBot = null;

        public PlayerInventory inv;

        #region SteamBot Configuration
        /**
		 * 
		 * SteamBot Configuration
		 * Modify this section to your needs
		 * 
		 */

        //Name of the Bot
        public string BotPersonaName = "TF2Scrap.com Bot 1";

        //Default Persona State
        public EPersonaState BotPersonaState = EPersonaState.LookingToTrade;

        #endregion


        //Hacking around https
        public bool CheckValidationResult(ServicePoint sp, X509Certificate certificate, WebRequest request, int error)
        {
            return true;
        }

        public void OnTradeFinished(bool success)
        {
        }

        public void PrintConsole(String line, ConsoleColor color = ConsoleColor.White, bool isDebug = false)
        {
            System.Console.ForegroundColor = color;
            if (isDebug)
            {
                if (FindArg(AllArgs, "-debug"))
                {
                    System.Console.WriteLine(line);
                }
            }
            else
            {
                System.Console.WriteLine(line);
            }
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        public Bot(BotInfo myBotInfo)
        {
            #region SteamRE Init

            theBot = myBotInfo;

            //Hacking around https
            ServicePointManager.CertificatePolicy = this;
            Console.ForegroundColor = ConsoleColor.White;

            steamClient = new SteamClient();
            steamTrade = steamClient.GetHandler<SteamTrading>();
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            steamCoordinator = steamClient.GetHandler<SteamGameCoordinator>();

            steamClient.Connect();
            #endregion

            while (true)
            {
                Update();
            }


        } //end Main method

        #region Misc Methods

        public void HandleSteamMessage(CallbackMsg msg)
        {
            #region Logged Off Handler
            msg.Handle<SteamUser.LoggedOffCallback>(callback => PrintConsole("[SteamRE] Logged Off: " + callback.Result, ConsoleColor.Magenta));
            #endregion

            #region Steam Disconnect Handler
            msg.Handle<SteamClient.DisconnectedCallback>(callback =>
            {
                isLoggedIn = false;
                if (trade != null)
                {
                    trade.cleanTrade();
                    trade = null;
                }
                PrintConsole("[SteamRE] Disconnected from Steam Network!", ConsoleColor.Magenta);
                steamClient.Connect();
            });
            #endregion

            #region Steam Connect Handler

            /**
				 * --Steam Connection Callback
				 * 
				 * It's not needed to modify this section
				 */

            msg.Handle<SteamUser.LoginKeyCallback>(callback =>
            {

                while (true)
                {
                    if (Authenticate(callback))
                    {
                        PrintConsole("Authenticated.");
                        break;
                    }
                    else
                    {
                        PrintConsole("Retrying auth...", ConsoleColor.Red);
                        Thread.Sleep(2000);
                    }
                }

                PrintConsole("Downloading schema and inventory...", ConsoleColor.Magenta);

                inv = GetInventory();
                TradeSystem.GetSchema();

                PrintConsole("All Done!", ConsoleColor.Magenta);

                //Get Name
                string name = theBot.DisplayName;

                //Set community status
                Console.Title = "Raffle Bot - " + name;
                steamFriends.SetPersonaName(name);
                steamFriends.SetPersonaState(BotPersonaState);

                //Done!
                PrintConsole("Successfully Logged In!\nWelcome " + steamUser.SteamID + "\n\n", ConsoleColor.Magenta);

                isLoggedIn = true;
            });

            msg.Handle<SteamClient.ConnectedCallback>(callback =>
            {
                //Print Callback
                PrintConsole("Connection Callback: " + callback.Result, ConsoleColor.Magenta);

                //Validate Result
                if (callback.Result == EResult.OK)
                {
                    string user = "";
                    string pass = "";

                    user = theBot.Username;
                    pass = theBot.Password;

                    if (user == "")
                    {
                        Console.WriteLine("Username: ");
                        user = Console.ReadLine();
                        Console.WriteLine("Password: ");
                        Console.BackgroundColor = ConsoleColor.Black;
                        pass = Console.ReadLine();
                    }

                    steamUser.LogOn(new SteamUser.LogOnDetails
                    {
                        Username = user,
                        Password = pass
                    });
                }
                else
                {

                    //Failure
                    PrintConsole("Failed to Connect to the steam community!\n", ConsoleColor.Red);
                    steamClient.Connect();
                    //return;
                }

            });
            #endregion

            #region Steam Login Handler
            //Logged in (or not)
            msg.Handle<SteamUser.LoggedOnCallback>(callback =>
            {
                PrintConsole("Logged on callback: " + callback.Result, ConsoleColor.Magenta);

                if (callback.Result != EResult.OK)
                {
                    PrintConsole("Login Failure: " + callback.Result, ConsoleColor.Red);
                }
                else
                {
                    //Download Inventory
                }
            });
            #endregion

            #region Steam Trade Start
            /**
				 * 
				 * Steam Trading Handler
				 *  
				 */
            msg.Handle<SteamTrading.TradeStartSessionCallback>(call =>
            {
                if (trade == null)
                {
                    OnTradeFinished(false);
                    return;
                }
                trade.initTrade(this, steamUser.SteamID, call.Other, WebCookies);
            });
            #endregion

            #region Trade Requested Handler
            //Don't modify this
            msg.Handle<SteamTrading.TradeProposedCallback>(thing =>
            {
                steamTrade.RequestTrade(thing.Other);
                trade = new TradeEnterRaffle();
            });

            msg.Handle<SteamTrading.TradeRequestCallback>(thing =>
            {
                PrintConsole("Trade Status: " + thing.Status, ConsoleColor.Magenta);

                if (thing.Status == ETradeStatus.Accepted)
                {
                    PrintConsole("Trade accepted!", ConsoleColor.Magenta);

                    /*if (trade != null && trade.otherSID != null)
                        trade.cleanTrade();

                    trade = new TradeReverseBank();*/
                }
            });
            #endregion

            #region Friend state
            msg.Handle<SteamFriends.PersonaStateCallback>(callback =>
            {
                steamFriends.AddFriend(callback.FriendID);
            });
            #endregion

            #region Steam Chat Handler
            /**
				 * 
				 * Steam Chat Handler
				 * 
				 */
            msg.Handle<SteamFriends.FriendMsgCallback>(callback =>
            {
                //Type (emote or chat)
                EChatEntryType type = callback.EntryType;

                if (type == EChatEntryType.ChatMsg)
                {
                    //Message is a chat message
                    PrintConsole("[Chat] " + steamFriends.GetFriendPersonaName(callback.Sender) + ": " + callback.Message, ConsoleColor.Magenta);

                    string message = callback.Message;

                    string response = theBot.ChatResponse;
                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, response);

                    //Chat API coming soon

                }

            });
            #endregion
        }

        public void Update()
        {
            while (true)
            {
                CallbackMsg msg = steamClient.GetCallback(true);

                if (msg == null)
                    break;

                HandleSteamMessage(msg);
            }



            #region Actual updating
            if (trade != null)
            {
                Thread.Sleep(1000);
                trade.poll();
            }
            #endregion

        }

        public bool Authenticate(SteamUser.LoginKeyCallback callback)
        {
            sessionId = WebHelpers.EncodeBase64(callback.UniqueID.ToString());

            PrintConsole("Got login key, performing web auth...");

            using (dynamic userAuth = WebAPI.GetInterface("ISteamUserAuth"))
            {
                // generate an AES session key
                var sessionKey = CryptoHelper.GenerateRandomBlock(32);

                // rsa encrypt it with the public key for the universe we're on
                byte[] cryptedSessionKey = null;
                using (var rsa = new RSACrypto(KeyDictionary.GetPublicKey(steamClient.ConnectedUniverse)))
                {
                    cryptedSessionKey = rsa.Encrypt(sessionKey);
                }

                byte[] loginKey = new byte[20];
                Array.Copy(Encoding.ASCII.GetBytes(callback.LoginKey), loginKey, callback.LoginKey.Length);

                // aes encrypt the loginkey with our session key
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

                KeyValue authResult;

                try
                {
                    authResult = userAuth.AuthenticateUser(
                        steamid: steamClient.SteamID.ConvertToUInt64(),
                        sessionkey: WebHelpers.UrlEncode(cryptedSessionKey),
                        encrypted_loginkey: WebHelpers.UrlEncode(cryptedLoginKey),
                        method: "POST"
                    );
                }
                catch (Exception ex)
                {
                    return false;
                }

                token = authResult["token"].AsString();

                return true;
            }
        }

        //Don't modify this
        static bool FindArg(string[] args, string arg)
        {
            foreach (string potentialArg in args)
            {
                if (potentialArg.IndexOf(arg, StringComparison.OrdinalIgnoreCase) != -1)
                    return true;
            }
            return false;
        }

        private PlayerInventory GetInventory()
        {
            try
            {
                var request = TradeSystem.CreateSteamRequest("http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=DF02BD82CF054DE26631BF1DEA9FCDE0&steamid=" + steamClient.SteamID.ConvertToUInt64(), this, "GET", false);
                HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
                Stream str = resp.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                string res = reader.ReadToEnd();

                return JsonConvert.DeserializeObject<PlayerInventory>(res);
            }
            catch (NullReferenceException c)
            {
                PrintConsole("Problem! Null Reference Exception", ConsoleColor.Red);
                return new PlayerInventory();
            }
            catch (WebException x)
            {
                PrintConsole("Problem! Steam API returned: " + x.Status, ConsoleColor.Red);
                return new PlayerInventory();
            }
        }

        public string GetSessionId()
        {
            return SteamWeb.getSession(new CookieContainer());

        }

        #endregion


    } //end class


} //end namespace