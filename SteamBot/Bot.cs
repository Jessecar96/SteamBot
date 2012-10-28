using System;
using System.Text;
using System.Net;
using System.Threading;
using SteamKit2;
using System.Collections.Generic;

namespace SteamBot
{
    public class Bot
    {
        public bool IsLoggedIn = false;

        public string DisplayName { get; private set; }
        public string ChatResponse;
        public ulong[] Admins;

        public SteamFriends SteamFriends;
        public SteamClient SteamClient;
        public SteamTrading SteamTrade;
        public SteamUser SteamUser;

        public Trade CurrentTrade;

        public bool IsDebugMode = false;

        public Log log;

        public delegate UserHandler UserHandlerCreator(Bot bot, SteamID id);
        public UserHandlerCreator CreateHandler;
        Dictionary<ulong, UserHandler> userHandlers = new Dictionary<ulong, UserHandler>();

        List<SteamID> friends = new List<SteamID>();

        string Username;
        string Password;
        string AuthCode;
        string apiKey;
        string sessionId;
        string token;

        public Bot(Configuration.BotInfo config, string apiKey, UserHandlerCreator handlerCreator, bool debug = false)
        {
            Username     = config.Username;
            Password     = config.Password;
            DisplayName  = config.DisplayName;
            ChatResponse = config.ChatResponse;
            Admins       = config.Admins;
            this.apiKey  = apiKey;
            AuthCode     = null;
            log          = new Log (config.LogFile, this);
            CreateHandler = handlerCreator;

            // Hacking around https
            ServicePointManager.ServerCertificateValidationCallback += SteamWeb.ValidateRemoteCertificate;

            log.Debug ("Initializing Steam Bot...");
            SteamClient = new SteamClient();
            SteamTrade = SteamClient.GetHandler<SteamTrading>();
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();
            log.Info ("Connecting...");
            SteamClient.Connect();

            Thread CallbackThread = new Thread(() => // Callback Handling
                       {
                while (true)
                {
                    CallbackMsg msg = SteamClient.WaitForCallback (true);
                    HandleSteamMessage (msg);
                }
            });

            new Thread(() => // Trade Polling if needed
                       {
                while (true)
                {
                    Thread.Sleep (800);
                    if (CurrentTrade != null)
                    {
                        try
                        {
                            CurrentTrade.Poll ();
                        }
                        catch (Exception e)
                        {
                            log.Error ("Error Polling Trade: " + e);
                            //Console.Write ("Error polling the trade: ");
                            //Console.WriteLine (e);
                        }
                    }
                }
            }).Start ();

            CallbackThread.Start();
            log.Success ("Done Loading Bot!");
            CallbackThread.Join();
        }

        void HandleSteamMessage (CallbackMsg msg)
        {
            log.Debug(msg.ToString());

            #region Login
            msg.Handle<SteamClient.ConnectedCallback> (callback =>
            {
                //PrintConsole ("Connection Callback: " + callback.Result, ConsoleColor.Magenta);
                log.Debug ("Connection Callback: " + callback.Result);

                if (callback.Result == EResult.OK)
                {
                    SteamUser.LogOn (new SteamUser.LogOnDetails
                         {
                        Username = Username,
                        Password = Password,
                        AuthCode = AuthCode
                    });
                }
                else
                {
                    log.Error ("Failed to connect to Steam Community, trying again...");
                    //PrintConsole ("Failed to Connect to the steam community!\n", ConsoleColor.Red);
                    SteamClient.Connect ();
                }

            });

            msg.Handle<SteamUser.LoggedOnCallback> (callback =>
            {
                log.Debug ("Logged On Callback: " + callback.Result);
                //PrintConsole ("Logged on callback: " + callback.Result, ConsoleColor.Magenta);

                if (callback.Result != EResult.OK)
                {
                    log.Error ("Login Error: " + callback.Result);
                    //PrintConsole("Login Failure: " + callback.Result, ConsoleColor.Red);
                }

                if (callback.Result == EResult.AccountLogonDenied)
                {
                    //PrintConsole("This account is protected by Steam Guard. Enter the authentication code sent to the associated email address", ConsoleColor.DarkYellow);
                    log.Interface ("This account is protected by Steam Guard.  Enter the authentication code sent to the proper email: ");
                    AuthCode = Console.ReadLine();
                }
            });

            msg.Handle<SteamUser.LoginKeyCallback> (callback =>
            {
                while (true)
                {
                    if (Authenticate (callback))
                    {
                        log.Success ("User Authenticated!");
                        //PrintConsole ("Authenticated.");
                        break;
                    }
                    else
                    {
                        log.Warn ("Authentication failed, retrying in 2s...");
                        //PrintConsole ("Retrying auth...", ConsoleColor.Red);
                        Thread.Sleep (2000);
                    }
                }

                //PrintConsole ("Downloading schema...", ConsoleColor.Magenta);
                log.Info ("Downloading Schema...");

                Trade.CurrentSchema = Schema.FetchSchema (apiKey);

                //PrintConsole ("All Done!", ConsoleColor.Magenta);
                log.Success ("Schema Downloaded!");

                SteamFriends.SetPersonaName ("[SteamBot] "+DisplayName);
                SteamFriends.SetPersonaState (EPersonaState.LookingToTrade);

                log.Success ("Steam Bot Logged In Completely!");
                //PrintConsole ("Successfully Logged In!\nWelcome " + SteamUser.SteamID + "\n\n", ConsoleColor.Magenta);

                IsLoggedIn = true;
            });
            #endregion

            #region Friends
            msg.Handle<SteamFriends.FriendsListCallback> (callback => 
            {
                foreach (SteamFriends.FriendsListCallback.Friend friend in callback.FriendList) 
                {
                    if (!friends.Contains(friend.SteamID)) 
                    {
                        friends.Add(friend.SteamID);
                        if (friend.Relationship == EFriendRelationship.PendingInvitee &&
                            getHandler(friend.SteamID).OnFriendAdd()) 
                        {
                            SteamFriends.AddFriend (friend.SteamID);
                        }
                    }
                }
            });

            msg.Handle<SteamFriends.FriendMsgCallback> (callback =>
            {
                EChatEntryType type = callback.EntryType;

                log.Info (String.Format ("Chat Message from {0}: {1}",
                                         SteamFriends.GetFriendPersonaName (callback.Sender),
                                         callback.Message
                                         ));

                getHandler(callback.Sender).OnMessage(callback.Message, type);

            });
            #endregion

            #region Trading
            msg.Handle<SteamTrading.TradeStartSessionCallback> (call =>
            {
                CurrentTrade = new Trade (SteamUser.SteamID, call.Other, sessionId, token, apiKey, this);
                CurrentTrade.OnTimeout += () => {
                    CurrentTrade = null;
                };
                getHandler(call.Other).SubscribeTrade(CurrentTrade);
            });

            msg.Handle<SteamTrading.TradeCancelRequestCallback> (call =>
            {
                log.Info ("Cancel Callback Request detected");
                getHandler(call.Other).UnsubscribeTrade(CurrentTrade);
                CurrentTrade = null;
            });

            msg.Handle<SteamTrading.TradeProposedCallback> (thing =>
            {
                if (getHandler(thing.Other).OnTradeRequest())
                    SteamTrade.RequestTrade (thing.Other);
            });

            msg.Handle<SteamTrading.TradeRequestCallback> (thing =>
            {
                log.Debug ("Trade Status: "+ thing.Status);
                //PrintConsole ("Trade Status: " + thing.Status, ConsoleColor.Magenta);

                if (thing.Status == ETradeStatus.Accepted)
                {
                    log.Info ("Trade Accepted!");
                    //PrintConsole ("Trade accepted!", ConsoleColor.Magenta);
                }

                if (thing.Status == ETradeStatus.Cancelled)
                {
                    log.Info ("Trade was cancelled");
                    CurrentTrade = null;
                }
            });
            #endregion

            #region Disconnect
            msg.Handle<SteamUser.LoggedOffCallback> (callback =>
            {
                IsLoggedIn = false;
                log.Warn ("Logged Off: " + callback.Result);
                //PrintConsole ("[SteamRE] Logged Off: " + callback.Result, ConsoleColor.Magenta);
            });

            msg.Handle<SteamClient.DisconnectedCallback> (callback =>
            {
                IsLoggedIn = false;
                if (CurrentTrade != null)
                {
                    CurrentTrade = null;
                }
                log.Warn ("Disconnected from Steam Network!");
                //PrintConsole ("[SteamRE] Disconnected from Steam Network!", ConsoleColor.Magenta);
                SteamClient.Connect ();
            });
            #endregion
        }

        // Authenticate. This does the same as SteamWeb.DoLogin(),
        // but without contacting the Steam Website.
        // Should this one doesnt work anymore, use SteamWeb.DoLogin().
        bool Authenticate (SteamUser.LoginKeyCallback callback)
        {
            sessionId = WebHelpers.EncodeBase64 (callback.UniqueID.ToString ());

            //PrintConsole ("Got login key, performing web auth...");

            using (dynamic userAuth = WebAPI.GetInterface ("ISteamUserAuth"))
            {
                // generate an AES session key
                var sessionKey = CryptoHelper.GenerateRandomBlock (32);

                // rsa encrypt it with the public key for the universe we're on
                byte[] cryptedSessionKey = null;
                using (RSACrypto rsa = new RSACrypto (KeyDictionary.GetPublicKey (SteamClient.ConnectedUniverse)))
                {
                    cryptedSessionKey = rsa.Encrypt (sessionKey);
                }

                byte[] loginKey = new byte[20];
                Array.Copy (Encoding.ASCII.GetBytes (callback.LoginKey), loginKey, callback.LoginKey.Length);

                // aes encrypt the loginkey with our session key
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt (loginKey, sessionKey);

                KeyValue authResult;

                try
                {
                    authResult = userAuth.AuthenticateUser (
                        steamid: SteamClient.SteamID.ConvertToUInt64 (),
                        sessionkey: WebHelpers.UrlEncode (cryptedSessionKey),
                        encrypted_loginkey: WebHelpers.UrlEncode (cryptedLoginKey),
                        method: "POST"
                    );
                }
                catch (Exception)
                {
                    return false;
                }

                token = authResult ["token"].AsString ();

                return true;
            }
        }

        UserHandler getHandler(SteamID sid) {
            if (!userHandlers.ContainsKey(sid)) {
                userHandlers[sid.ConvertToUInt64()] = CreateHandler(this, sid);
            }
            return userHandlers[sid.ConvertToUInt64()];;
        }

    }
}
