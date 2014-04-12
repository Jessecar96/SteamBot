using System;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
using SteamBot.SteamGroups;
using SteamKit2;
using SteamTrade;
using SteamKit2.Internal;

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
        public SteamGameCoordinator SteamGameCoordinator;

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

        // List of Steam groups the bot is in.
        private readonly List<SteamID> groups = new List<SteamID>();

        // The maximum amount of time the bot will trade for.
        public int MaximumTradeTime { get; private set; }

        // The maximum amount of time the bot will wait in between
        // trade actions.
        public int MaximiumActionGap { get; private set; }

        //The current game that the bot is playing, for posterity.
        public int CurrentGame = 0;

        // The Steam Web API key.
        public string apiKey;

        // The prefix put in the front of the bot's display name.
        string DisplayNamePrefix;

        // Log level to use for this bot
        Log.LogLevel LogLevel;

        // The number, in milliseconds, between polls for the trade.
        int TradePollingInterval;

        public string MyLoginKey;
        string sessionId;
        string token;
        bool isprocess;
        public bool IsRunning = false;

        public string AuthCode { get; set; }

        SteamUser.LogOnDetails logOnDetails;

        TradeManager tradeManager;
        private Task<Inventory> myInventoryTask;

        public Inventory MyInventory
        {
            get
            {
                myInventoryTask.Wait();
                return myInventoryTask.Result;
            }
        }

        private BackgroundWorker backgroundWorker;

        public Bot(Configuration.BotInfo config, string apiKey, UserHandlerCreator handlerCreator, bool debug = false, bool process = false)
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
            this.isprocess = process;
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
            SteamGameCoordinator = SteamClient.GetHandler<SteamGameCoordinator>();

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
            IsRunning = true;

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
            IsRunning = false;

            log.Debug("Trying to shut down bot thread.");
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

        public void HandleBotCommand(string command)
        {
            try
            {
                GetUserHandler(SteamClient.SteamID).OnBotCommand(command);
            }
            catch (ObjectDisposedException e)
            {
                // Writing to console because odds are the error was caused by a disposed log.
                Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
                if (!this.IsRunning)
                {
                    Console.WriteLine("The Bot is no longer running and could not write to the log. Try Starting this bot first.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
            }
        }

        bool HandleTradeSessionStart (SteamID other)
        {
            if (CurrentTrade != null)
                return false;

            try
            {
                tradeManager.InitializeTrade(SteamUser.SteamID, other);
                CurrentTrade = tradeManager.CreateTrade (SteamUser.SteamID, other);
                CurrentTrade.OnClose += CloseTrade;
                SubscribeTrade(CurrentTrade, GetUserHandler(other));

                tradeManager.StartTradeThread(CurrentTrade);
                return true;
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
        }

        public void SetGamePlaying(int id)
        {
            var gamePlaying = new SteamKit2.ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            if (id != 0)
                gamePlaying.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
                {
                    game_id = new GameID(id),
                });

            SteamClient.Send(gamePlaying);

            CurrentGame = id;
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

                if (callback.Result == EResult.OK)
                {
                    MyLoginKey = callback.WebAPIUserNonce;
                }
                else
                {
                    log.Error ("Login Error: " + callback.Result);
                }

                if (callback.Result == EResult.AccountLogonDenied)
                {
                    log.Interface ("This account is SteamGuard enabled. Enter the code via the `auth' command.");

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
                    log.Interface("The given SteamGuard code was invalid. Try again using the `auth' command.");
                    logOnDetails.AuthCode = Console.ReadLine();
                }
            });

            msg.Handle<SteamUser.LoginKeyCallback> (callback =>
            {
                while (true)
                {
                    bool authd = SteamWeb.Authenticate(callback, SteamClient, out sessionId, out token, MyLoginKey);

                    if (authd)
                    {
                        log.Success ("User Authenticated!");

                        tradeManager = new TradeManager(apiKey, sessionId, token);
                        tradeManager.SetTradeTimeLimits(MaximumTradeTime, MaximiumActionGap, TradePollingInterval);
                        tradeManager.OnTimeout += OnTradeTimeout;
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

                GetUserHandler(SteamClient.SteamID).OnLoginCompleted();
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
            msg.Handle<SteamFriends.FriendsListCallback>(callback =>
            {
                foreach (SteamFriends.FriendsListCallback.Friend friend in callback.FriendList)
                {
                    if (friend.SteamID.AccountType == EAccountType.Clan)
                    {
                        if (!groups.Contains(friend.SteamID))
                        {
                            groups.Add(friend.SteamID);
                            if (friend.Relationship == EFriendRelationship.RequestRecipient)
                            {
                                if (GetUserHandler(friend.SteamID).OnGroupAdd())
                                {
                                    AcceptGroupInvite(friend.SteamID);
                                }
                                else
                                {
                                    DeclineGroupInvite(friend.SteamID);
                                }
                            }
                        }
                        else
                        {
                            if (friend.Relationship == EFriendRelationship.None)
                            {
                                groups.Remove(friend.SteamID);
                            }
                        }
                    }
                    else if (friend.SteamID.AccountType != EAccountType.Clan)
                    {
                    if (!friends.Contains(friend.SteamID))
                    {
                        friends.Add(friend.SteamID);
                        if (friend.Relationship == EFriendRelationship.RequestRecipient &&
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
                }
            });


            msg.Handle<SteamFriends.FriendMsgCallback> (callback =>
            {
                EChatEntryType type = callback.EntryType;

                if (callback.EntryType == EChatEntryType.ChatMsg)
                {
                    log.Info (String.Format ("Chat Message from {0}: {1}",
                                         SteamFriends.GetFriendPersonaName (callback.Sender),
                                         callback.Message
                                         ));
                    GetUserHandler(callback.Sender).OnMessage(callback.Message, type);
                }
            });
            #endregion

            #region Group Chat
            msg.Handle<SteamFriends.ChatMsgCallback>(callback =>
            {
                GetUserHandler(callback.ChatterID).OnChatRoomMessage(callback.ChatRoomID, callback.ChatterID, callback.Message);
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
                catch (WebException we)
                {                 
                    SteamFriends.SendChatMessage(callback.OtherClient,
                             EChatEntryType.ChatMsg,
                             "Trade error: " + we.Message);

                    SteamTrade.RespondToTrade(callback.TradeID, false);
                    return;
                }
                catch (Exception)
                {
                    SteamFriends.SendChatMessage(callback.OtherClient,
                             EChatEntryType.ChatMsg,
                             "Trade declined. Could not correctly fetch your backpack.");

                    SteamTrade.RespondToTrade(callback.TradeID, false);
                    return;
                }

                //if (tradeManager.OtherInventory.IsPrivate)
                //{
                //    SteamFriends.SendChatMessage(callback.OtherClient, 
                //                                 EChatEntryType.ChatMsg,
                //                                 "Trade declined. Your backpack cannot be private.");

                //    SteamTrade.RespondToTrade (callback.TradeID, false);
                //    return;
                //}

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
                    GetUserHandler(callback.OtherClient).OnTradeRequestReply(true, callback.Response.ToString());
                }
                else
                {
                    log.Warn ("Trade failed: " + callback.Response);
                    CloseTrade ();
                    GetUserHandler(callback.OtherClient).OnTradeRequestReply(false, callback.Response.ToString());
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
            Directory.CreateDirectory(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "sentryfiles"));
            FileInfo fi = new FileInfo(System.IO.Path.Combine("sentryfiles",String.Format("{0}.sentryfile", logOnDetails.Username)));

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

            Directory.CreateDirectory(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "sentryfiles"));

            File.WriteAllBytes (System.IO.Path.Combine("sentryfiles", String.Format("{0}.sentryfile", logOnDetails.Username)), machineAuth.Data);
            
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
            myInventoryTask = Task.Factory.StartNew(() => Inventory.FetchInventory(SteamUser.SteamID, apiKey));
        }

        /// <summary>
        /// Subscribes all listeners of this to the trade.
        /// </summary>
        public void SubscribeTrade (Trade trade, UserHandler handler)
        {
            trade.OnSuccess += handler.OnTradeSuccess;
            trade.OnClose += handler.OnTradeClose;
            trade.OnError += handler.OnTradeError;
            //trade.OnTimeout += OnTradeTimeout;
            trade.OnAfterInit += handler.OnTradeInit;
            trade.OnUserAddItem += handler.OnTradeAddItem;
            trade.OnUserRemoveItem += handler.OnTradeRemoveItem;
            trade.OnMessage += handler.OnTradeMessage;
            trade.OnUserSetReady += handler.OnTradeReadyHandler;
            trade.OnUserAccept += handler.OnTradeAcceptHandler;
        }
        
        /// <summary>
        /// Unsubscribes all listeners of this from the current trade.
        /// </summary>
        public void UnsubscribeTrade (UserHandler handler, Trade trade)
        {
            trade.OnSuccess -= handler.OnTradeSuccess;
            trade.OnClose -= handler.OnTradeClose;
            trade.OnError -= handler.OnTradeError;
            //Trade.OnTimeout -= OnTradeTimeout;
            trade.OnAfterInit -= handler.OnTradeInit;
            trade.OnUserAddItem -= handler.OnTradeAddItem;
            trade.OnUserRemoveItem -= handler.OnTradeRemoveItem;
            trade.OnMessage -= handler.OnTradeMessage;
            trade.OnUserSetReady -= handler.OnTradeReadyHandler;
            trade.OnUserAccept -= handler.OnTradeAcceptHandler;
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
            CallbackMsg msg;

            while (!backgroundWorker.CancellationPending)
            {
                try
                {
                    msg = SteamClient.WaitForCallback(true);
                    HandleSteamMessage(msg);
                }
                catch (WebException e)
                {
                    log.Error("URI: " + (e.Response != null && e.Response.ResponseUri != null ? e.Response.ResponseUri.ToString() : "unknown") + " >> " + e.ToString());
                    System.Threading.Thread.Sleep(45000);//Steam is down, retry in 45 seconds.
                }
                catch (Exception e)
                {
                    log.Error(e.ToString());
                    log.Warn("Restarting bot...");
                }
            }
        }

        #endregion Background Worker Methods

        private void FireOnSteamGuardRequired(SteamGuardRequiredEventArgs e)
        {
            // Set to null in case this is another attempt
            this.AuthCode = null;

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

        #region Group Methods

        /// <summary>
        /// Accepts the invite to a Steam Group
        /// </summary>
        /// <param name="group">SteamID of the group to accept the invite from.</param>
        private void AcceptGroupInvite(SteamID group)
        {
            var AcceptInvite = new ClientMsg<CMsgGroupInviteAction>((int)EMsg.ClientAcknowledgeClanInvite);

            AcceptInvite.Body.GroupID = group.ConvertToUInt64();
            AcceptInvite.Body.AcceptInvite = true;

            this.SteamClient.Send(AcceptInvite);
            
        }

        /// <summary>
        /// Declines the invite to a Steam Group
        /// </summary>
        /// <param name="group">SteamID of the group to decline the invite from.</param>
        private void DeclineGroupInvite(SteamID group)
        {
            var DeclineInvite = new ClientMsg<CMsgGroupInviteAction>((int)EMsg.ClientAcknowledgeClanInvite);

            DeclineInvite.Body.GroupID = group.ConvertToUInt64();
            DeclineInvite.Body.AcceptInvite = false;

            this.SteamClient.Send(DeclineInvite);
        }

        /// <summary>
        /// Invites a use to the specified Steam Group
        /// </summary>
        /// <param name="user">SteamID of the user to invite.</param>
        /// <param name="groupId">SteamID of the group to invite the user to.</param>
        public void InviteUserToGroup(SteamID user, SteamID groupId)
        {
            var InviteUser = new ClientMsg<CMsgInviteUserToGroup>((int)EMsg.ClientInviteUserToClan);

            InviteUser.Body.GroupID = groupId.ConvertToUInt64();
            InviteUser.Body.Invitee = user.ConvertToUInt64();
            InviteUser.Body.UnknownInfo = true;

            this.SteamClient.Send(InviteUser);
        }

        #endregion
    }
}
