using System;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
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
        string _authcode;

        public string AuthCode
        {
            get { return this._authcode; }

            set
            {
                this._authcode = value;
            }
        }

        SteamUser.LogOnDetails logOnDetails;

        TradeManager tradeManager;

        public Inventory MyInventory;
        public Inventory OtherInventory;

        private BackgroundWorker backgroundWorker;

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

            backgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;
            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Occurs when the bot needs the SteamGuard authentication code.
        /// </summary>
        /// <remarks>
        /// Return the code in <see cref="SteamGuardRequiredEventArgs.SteamGuard"/>
        /// </remarks>
        public event EventHandler<SteamGuardRequiredEventArgs> OnSteamGuardRequired;

        /// <summary>
        /// Starts the callback thread and connects to Steam via SteamKit2.
        /// </summary>
        /// <remarks>
        /// THIS NEVER RETURNS.
        /// </remarks>
        /// <returns><c>true</c>. See remarks</returns>
        public bool StartBot()
        {
            log.Info("Connecting...");

            if (!backgroundWorker.IsBusy)
                // background worker is not running
                backgroundWorker.RunWorkerAsync();

            SteamClient.Connect();
            
            log.Success("Done Loading Bot!");

            return true; // never get here
        }

        /// <summary>
        /// Disconnect from the Steam network and stop the callback
        /// thread.
        /// </summary>
        public void StopBot()
        {
            log.Debug("Tryring to shut down bot thread.");
            SteamClient.Disconnect();

            backgroundWorker.CancelAsync();
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
                    response = "Trade failed. Could not correctly fetch your backpack. Either the inventory is inaccessible or your backpack is private.";
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

                    // try to get the steamguard auth code from the event callback
                    var eva = new SteamGuardRequiredEventArgs();
                    FireOnSteamGuardRequired(eva);
                    if (!String.IsNullOrEmpty(eva.SteamGuard))
                        logOnDetails.AuthCode = eva.SteamGuard;
                    else
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
                            SteamFriends.AddFriend(friend.SteamID);
                        }
                    }
                    else
                    {
                        if (friend.Relationship == EFriendRelationship.None)
                        {
                            friends.Remove(friend.SteamID);
                            GetUserHandler(friend.SteamID).OnFriendRemove();
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
                if (callback.Response == EEconTradeResponse.Accepted)
                {
                    log.Debug ("Trade Status: " + callback.Response);
                    log.Info ("Trade Accepted!");
                }
                else
                {
                    log.Warn ("Trade failed: " + callback.Response);
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
        /// Gets the bot's inventory and stores it in MyInventory.
        /// </summary>
        /// <example> This sample shows how to find items in the bot's inventory from a user handler.
        /// <code>
        /// Bot.GetInventory(); // Get the inventory first
        /// foreach (var item in Bot.MyInventory.Items)
        /// {
        ///     if (item.Defindex == 5021)
        ///     {
        ///         // Bot has a key in its inventory
        ///     }
        /// }
        /// </code>
        /// </example>
        public void GetInventory()
        {
            MyInventory = Inventory.FetchInventory(SteamUser.SteamID, apiKey);
        }

        /// <summary>
        /// Gets the other user's inventory and stores it in OtherInventory.
        /// </summary>
        /// <param name="OtherSID">The SteamID of the other user</param>
        /// <example> This sample shows how to find items in the other user's inventory from a user handler.
        /// <code>
        /// Bot.GetOtherInventory(OtherSID); // Get the inventory first
        /// foreach (var item in Bot.OtherInventory.Items)
        /// {
        ///     if (item.Defindex == 5021)
        ///     {
        ///         // User has a key in its inventory
        ///     }
        /// }
        /// </code>
        /// </example>
        public void GetOtherInventory(SteamID OtherSID)
        {
            OtherInventory = Inventory.FetchInventory(OtherSID, apiKey);
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

        #region Background Worker Methods

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            if (runWorkerCompletedEventArgs.Error != null)
            {
                Exception ex = runWorkerCompletedEventArgs.Error;

                var s = string.Format("Unhandled exceptions in bot {0} callback thread: {1} {2}",
                      DisplayName,
                      Environment.NewLine,
                      ex);
                log.Error(s);

                log.Info("This bot died. Stopping it..");
                //backgroundWorker.RunWorkerAsync();
                //Thread.Sleep(10000);
                StopBot();
                //StartBot();
            }

            log.Dispose();
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            while (!backgroundWorker.CancellationPending)
            {
                CallbackMsg msg = SteamClient.WaitForCallback(true);
                HandleSteamMessage(msg);
            }
        }

        #endregion Background Worker Methods

        private void FireOnSteamGuardRequired(SteamGuardRequiredEventArgs e)
        {
            EventHandler<SteamGuardRequiredEventArgs> handler = OnSteamGuardRequired;
            if (handler != null)
                handler(this, e);
            else
            {
                while (true)
                {
                    if (this.AuthCode != null)
                    {
                        e.SteamGuard = this.AuthCode;
                        break;
                    }

                    Thread.Sleep(5);
                }
            }
        }
    }
}
