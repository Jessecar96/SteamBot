using System;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using SteamKit2;
using SteamTrade;

namespace SteamBot
{
    public class Bot
    {
        public string BotControlClass;
        // If the bot is logged in fully or not.  This is only set
        // when it is.
        public bool IsLoggedIn = false;

        // The bot's display name.  Changing this does not mean that
        // the bot's name will change.
        public string DisplayName { get; private set; }

        // The response to all chat messages sent to it.
        public string ChatResponse;

        // A list of SteamIDs that this bot recognizes as admins.
        public ulong[] Admins;
        public SteamFriends SteamFriends;
        public SteamClient SteamClient;
        public SteamTrading SteamTrade;
        public SteamUser SteamUser;

        // The current trade; if the bot is not in a trade, this is
        // null.
        public Trade CurrentTrade;

        public bool IsDebugMode = false;

        // The log for the bot.  This logs with the bot's display name.
        public Log log;

        public delegate UserHandler UserHandlerCreator(Bot bot, SteamID id);
        public UserHandlerCreator CreateHandler;
        Dictionary<ulong, UserHandler> userHandlers = new Dictionary<ulong, UserHandler>();

        List<SteamID> friends = new List<SteamID>();

        // The maximum amount of time the bot will trade for.
        public int MaximumTradeTime { get; private set; }

        // The maximum amount of time the bot will wait in between
        // trade actions.
        public int MaximiumActionGap { get; private set; }

        // The Steam Web API key.
        string apiKey;

        // The prefix put in the front of the bot's display name.
        string DisplayNamePrefix;

        // Log level to use for this bot
        Log.LogLevel LogLevel;

        // The number, in milliseconds, between polls for the trade.
        int TradePollingInterval;

        string sessionId;
        string token;

        SteamUser.LogOnDetails logOnDetails;

        TradeManager tradeManager;

        public Bot(Configuration.BotInfo config, string apiKey, UserHandlerCreator handlerCreator, bool debug = false)
        {
            logOnDetails = new SteamUser.LogOnDetails
            {
                Username = config.Username,
                Password = config.Password
            };
            DisplayName  = config.DisplayName;
            ChatResponse = config.ChatResponse;
            MaximumTradeTime = config.MaximumTradeTime;
            MaximiumActionGap = config.MaximumActionGap;
            DisplayNamePrefix = config.DisplayNamePrefix;
            TradePollingInterval = config.TradePollingInterval <= 100 ? 800 : config.TradePollingInterval;
            Admins       = config.Admins;
            this.apiKey  = apiKey;
            try
            {
                LogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.LogLevel, true);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid LogLevel provided in configuration. Defaulting to 'INFO'");
                LogLevel = Log.LogLevel.Info;
            }
            log          = new Log (config.LogFile, this.DisplayName, LogLevel);
            CreateHandler = handlerCreator;
            BotControlClass = config.BotControlClass;

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
            
            CallbackThread.Start();
            log.Success ("Done Loading Bot!");
            CallbackThread.Join();
        }

        /// <summary>
        /// Creates a new trade with the given partner.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if trade was opened,
        /// <c>false</c> if there is another trade that must be closed first.
        /// </returns>
        public bool OpenTrade (SteamID other)
        {
            if (CurrentTrade != null)
                return false;

            SteamTrade.Trade(other);

            return true;
        }

        /// <summary>
        /// Closes the current active trade.
        /// </summary>
        public void CloseTrade() 
        {
            if (CurrentTrade == null)
                return;

            UnsubscribeTrade (GetUserHandler (CurrentTrade.OtherSID), CurrentTrade);

            tradeManager.StopTrade ();

            CurrentTrade = null;
        }

        void OnTradeTimeout(object sender, EventArgs args) 
        {
            // ignore event params and just null out the trade.
            GetUserHandler (CurrentTrade.OtherSID).OnTradeTimeout();
        }

        void OnTradeEnded (object sender, EventArgs e)
        {
            CloseTrade();
        }        

        bool HandleTradeSessionStart (SteamID other)
        {
            if (CurrentTrade != null)
                return false;

            try
            {
                tradeManager.InitializeTrade(SteamUser.SteamID, other);
                CurrentTrade = tradeManager.StartTrade (SteamUser.SteamID, other);
            }
            catch (SteamTrade.Exceptions.InventoryFetchException ie)
            {
                // we shouldn't get here because the inv checks are also
                // done in the TradeProposedCallback handler.
                string response = String.Empty;
                
                if (ie.FailingSteamId.ConvertToUInt64() == other.ConvertToUInt64())
                {
                    response = "Trade failed. Could not correctly fetch your backpack. Either the inventory is inaccessable or your backpack is private.";
                }
                else 
                {
                    response = "Trade failed. Could not correctly fetch my backpack.";
                }
                
                SteamFriends.SendChatMessage(other, 
                                             EChatEntryType.ChatMsg,
                                             response);

                log.Info ("Bot sent other: " + response);
                
                CurrentTrade = null;
                return false;
            }
            
            CurrentTrade.OnClose += CloseTrade;
            SubscribeTrade (CurrentTrade, GetUserHandler (other));

            return true;
        }

        void HandleSteamMessage (CallbackMsg msg)
        {
            log.Debug(msg.ToString());

            #region Login
            msg.Handle<SteamClient.ConnectedCallback> (callback =>
            {
                log.Debug ("Connection Callback: " + callback.Result);

                if (callback.Result == EResult.OK)
                {
                    UserLogOn();
                }
                else
                {
                    log.Error ("Failed to connect to Steam Community, trying again...");
                    SteamClient.Connect ();
                }

            });

            msg.Handle<SteamUser.LoggedOnCallback> (callback =>
            {
                log.Debug ("Logged On Callback: " + callback.Result);

                if (callback.Result != EResult.OK)
                {
                    log.Error ("Login Error: " + callback.Result);
                }

                if (callback.Result == EResult.AccountLogonDenied)
                {
                    log.Interface ("This account is protected by Steam Guard.  Enter the authentication code sent to the proper email: ");
                    logOnDetails.AuthCode = Console.ReadLine();
                }

                if (callback.Result == EResult.InvalidLoginAuthCode)
                {
                    log.Interface("An Invalid Authorization Code was provided.  Enter the authentication code sent to the proper email: ");
                    logOnDetails.AuthCode = Console.ReadLine();
                }
            });

            msg.Handle<SteamUser.LoginKeyCallback> (callback =>
            {
                while (true)
                {
                    bool authd = SteamWeb.Authenticate(callback, SteamClient, out sessionId, out token);
                    if (authd)
                    {
                        log.Success ("User Authenticated!");

                        tradeManager = new TradeManager(apiKey, sessionId, token);
                        tradeManager.SetTradeTimeLimits(MaximumTradeTime, MaximiumActionGap, TradePollingInterval);
                        tradeManager.OnTimeout += OnTradeTimeout;
                        tradeManager.OnTradeEnded += OnTradeEnded;
                        break;
                    }
                    else
                    {
                        log.Warn ("Authentication failed, retrying in 2s...");
                        Thread.Sleep (2000);
                    }
                }

                if (Trade.CurrentSchema == null)
                {
                    log.Info ("Downloading Schema...");
                    Trade.CurrentSchema = Schema.FetchSchema (apiKey);
                    log.Success ("Schema Downloaded!");
                }

                SteamFriends.SetPersonaName (DisplayNamePrefix+DisplayName);
                SteamFriends.SetPersonaState (EPersonaState.Online);

                log.Success ("Steam Bot Logged In Completely!");

                IsLoggedIn = true;
            });

            // handle a special JobCallback differently than the others
            if (msg.IsType<SteamClient.JobCallback<SteamUser.UpdateMachineAuthCallback>>())
            {
                msg.Handle<SteamClient.JobCallback<SteamUser.UpdateMachineAuthCallback>>(
                    jobCallback => OnUpdateMachineAuthCallback(jobCallback.Callback, jobCallback.JobID)
                );
            }
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
                            GetUserHandler(friend.SteamID).OnFriendAdd())
                        {
                            SteamFriends.AddFriend (friend.SteamID);
                        }
                    }
                }
            });

            msg.Handle<SteamFriends.FriendMsgCallback> (callback =>
            {
                EChatEntryType type = callback.EntryType;

                if (callback.EntryType == EChatEntryType.ChatMsg ||
                    callback.EntryType == EChatEntryType.Emote)
                {
                    log.Info (String.Format ("Chat Message from {0}: {1}",
                                         SteamFriends.GetFriendPersonaName (callback.Sender),
                                         callback.Message
                                         ));
                    GetUserHandler(callback.Sender).OnMessage(callback.Message, type);
                }
            });
            #endregion

            #region Trading
            msg.Handle<SteamTrading.SessionStartCallback> (callback =>
            {
                bool started = HandleTradeSessionStart (callback.OtherClient);

                if (!started)
                    log.Error ("Could not start the trade session.");
                else
                    log.Debug ("SteamTrading.SessionStartCallback handled successfully. Trade Opened.");
            });

            msg.Handle<SteamTrading.TradeProposedCallback> (callback =>
            {
                try
                {
                    tradeManager.InitializeTrade(SteamUser.SteamID, callback.OtherClient);
                }
                catch 
                {
                    SteamFriends.SendChatMessage(callback.OtherClient, 
                                                 EChatEntryType.ChatMsg,
                                                 "Trade declined. Could not correctly fetch your backpack.");
                    
                    SteamTrade.RespondToTrade (callback.TradeID, false);
                    return;
                }

                if (tradeManager.OtherInventory.IsPrivate)
                {
                    SteamFriends.SendChatMessage(callback.OtherClient, 
                                                 EChatEntryType.ChatMsg,
                                                 "Trade declined. Your backpack cannot be private.");

                    SteamTrade.RespondToTrade (callback.TradeID, false);
                    return;
                }

                if (CurrentTrade == null && GetUserHandler (callback.OtherClient).OnTradeRequest ())
                    SteamTrade.RespondToTrade (callback.TradeID, true);
                else
                    SteamTrade.RespondToTrade (callback.TradeID, false);
            });

            msg.Handle<SteamTrading.TradeResultCallback> (callback =>
            {
                log.Debug ("Trade Status: "+ callback.Response);

                if (callback.Response == EEconTradeResponse.Accepted)
                {
                    log.Info ("Trade Accepted!");
                }
                if (callback.Response == EEconTradeResponse.Cancel ||
                    callback.Response == EEconTradeResponse.ConnectionFailed ||
                    callback.Response == EEconTradeResponse.Declined ||
                    callback.Response == EEconTradeResponse.Error ||
                    callback.Response == EEconTradeResponse.InitiatorAlreadyTrading ||
                    callback.Response == EEconTradeResponse.TargetAlreadyTrading ||
                    callback.Response == EEconTradeResponse.Timeout ||
                    callback.Response == EEconTradeResponse.TooSoon ||
                    callback.Response == EEconTradeResponse.VacBannedInitiator ||
                    callback.Response == EEconTradeResponse.VacBannedTarget ||
                    callback.Response == EEconTradeResponse.NotLoggedIn) // uh...
                {
                    CloseTrade ();
                }

            });
            #endregion

            #region Disconnect
            msg.Handle<SteamUser.LoggedOffCallback> (callback =>
            {
                IsLoggedIn = false;
                log.Warn ("Logged Off: " + callback.Result);
            });

            msg.Handle<SteamClient.DisconnectedCallback> (callback =>
            {
                IsLoggedIn = false;
                CloseTrade ();
                log.Warn ("Disconnected from Steam Network!");
                SteamClient.Connect ();
            });
            #endregion
        }

        void UserLogOn()
        {
            // get sentry file which has the machine hw info saved 
            // from when a steam guard code was entered
            FileInfo fi = new FileInfo(String.Format("{0}.sentryfile", logOnDetails.Username));

            if (fi.Exists && fi.Length > 0)
                logOnDetails.SentryFileHash = SHAHash(File.ReadAllBytes(fi.FullName));
            else
                logOnDetails.SentryFileHash = null;

            SteamUser.LogOn(logOnDetails);
        }

        UserHandler GetUserHandler (SteamID sid)
        {
            if (!userHandlers.ContainsKey (sid))
            {
                userHandlers [sid.ConvertToUInt64 ()] = CreateHandler (this, sid);
            }
            return userHandlers [sid.ConvertToUInt64 ()];
        }

        static byte [] SHAHash (byte[] input)
        {
            SHA1Managed sha = new SHA1Managed();
            
            byte[] output = sha.ComputeHash( input );
            
            sha.Clear();
            
            return output;
        }

        void OnUpdateMachineAuthCallback (SteamUser.UpdateMachineAuthCallback machineAuth, JobID jobId)
        {
            byte[] hash = SHAHash (machineAuth.Data);

            File.WriteAllBytes (String.Format ("{0}.sentryfile", logOnDetails.Username), machineAuth.Data);
            
            var authResponse = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,
                
                SentryFileHash = hash, // should be the sha1 hash of the sentry file we just wrote
                
                OneTimePassword = machineAuth.OneTimePassword, // not sure on this one yet, since we've had no examples of steam using OTPs
                
                LastError = 0, // result from win32 GetLastError
                Result = EResult.OK, // if everything went okay, otherwise ~who knows~
                
                JobID = jobId, // so we respond to the correct server job
            };
            
            // send off our response
            SteamUser.SendMachineAuthResponse (authResponse);
        }

        /// <summary>
        /// Subscribes all listeners of this to the trade.
        /// </summary>
        public void SubscribeTrade (Trade trade, UserHandler handler)
        {
            trade.OnClose += handler.OnTradeClose;
            trade.OnError += handler.OnTradeError;
            //trade.OnTimeout += OnTradeTimeout;
            trade.OnAfterInit += handler.OnTradeInit;
            trade.OnUserAddItem += handler.OnTradeAddItem;
            trade.OnUserRemoveItem += handler.OnTradeRemoveItem;
            trade.OnMessage += handler.OnTradeMessage;
            trade.OnUserSetReady += handler.OnTradeReady;
            trade.OnUserAccept += handler.OnTradeAccept;
        }
        
        /// <summary>
        /// Unsubscribes all listeners of this from the current trade.
        /// </summary>
        public void UnsubscribeTrade (UserHandler handler, Trade trade)
        {
            trade.OnClose -= handler.OnTradeClose;
            trade.OnError -= handler.OnTradeError;
            //Trade.OnTimeout -= OnTradeTimeout;
            trade.OnAfterInit -= handler.OnTradeInit;
            trade.OnUserAddItem -= handler.OnTradeAddItem;
            trade.OnUserRemoveItem -= handler.OnTradeRemoveItem;
            trade.OnMessage -= handler.OnTradeMessage;
            trade.OnUserSetReady -= handler.OnTradeReady;
            trade.OnUserAccept -= handler.OnTradeAccept;
        }
    }
}
