using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
using SteamBot.SteamGroups;
using SteamKit2;
using SteamAPI;
using SteamKit2.Internal;
using SteamAuth;

namespace SteamBot
{
    public class Bot : IDisposable
    {
        #region Bot delegates
        public delegate UserHandler UserHandlerCreator(Bot bot, SteamID id);
        #endregion

        #region Private readonly variables
        private readonly SteamUser.LogOnDetails logOnDetails;
        private readonly string logFile;
        private readonly Dictionary<SteamID, UserHandler> userHandlers;
        private readonly Log.LogLevel consoleLogLevel;
        private readonly Log.LogLevel fileLogLevel;
        private readonly UserHandlerCreator createHandler;
        private readonly bool isProccess;
        private readonly BackgroundWorker botThread;
        #endregion

        #region Private variables
        private string myUserNonce;
        private string myUniqueId;
        private bool cookiesAreInvalid = true;
        private List<SteamID> friends;
        private bool disposed = false;
        #endregion

        #region Public readonly variables
        /// <summary>
        /// Userhandler class bot is running.
        /// </summary>
        public readonly string BotControlClass;
        /// <summary>
        /// The display name of bot to steam.
        /// </summary>
        public readonly string DisplayName;
        /// <summary>
        /// The chat response from the config file.
        /// </summary>
        public readonly string ChatResponse;
        /// <summary>
        /// An array of admins for bot.
        /// </summary>
        public readonly IEnumerable<SteamID> Admins;
        public readonly SteamClient SteamClient;
        public readonly SteamUser SteamUser;
        public readonly SteamFriends SteamFriends;
        public readonly SteamTrading SteamTrade;
        public readonly SteamGameCoordinator SteamGameCoordinator;
        /// <summary>
        /// The amount of time the bot will trade for.
        /// </summary>
        public readonly int MaximumTradeTime;
        /// <summary>
        /// The amount of time the bot will wait between user interactions with trade.
        /// </summary>
        public readonly int MaximumActionGap;
        /// <summary>
        /// The api key of bot.
        /// </summary>
        public readonly string ApiKey;
        public readonly SteamAPI.SteamWeb SteamWeb;
        /// <summary>
        /// The prefix shown before bot's display name.
        /// </summary>
        public readonly string DisplayNamePrefix;

        /// <summary>
        /// The time in milliseconds between checking for new trade offers.
        /// </value>
        public readonly int TradeOfferRefreshRate;
        #endregion

        #region Public variables
        public string AuthCode;
        public bool IsRunning;

        /// <summary>
        /// Is bot fully Logged in.
        /// Set only when bot did successfully Log in.
        /// </summary>
        public bool IsLoggedIn { get; private set; }

        /// <summary>
        /// The current game bot is in.
        /// Default: 0 = No game.
        /// </summary>
        public int CurrentGame { get; private set; }

        /// <summary>
        /// The instance of the Logger for the bot.
        /// </summary>
        public Log Log;

        public TradeOffers TradeOffers;
        public SteamGuardAccount SteamGuardAccount;
        #endregion

        public IEnumerable<SteamID> FriendsList
        {
            get
            {
                CreateFriendsListIfNecessary();
                return friends;
            }
        }

        /// <summary>
        /// Compatibility sanity.
        /// </summary>
        [Obsolete("Refactored to be Log instead of log")]
        public Log log { get { return Log; } }

        public Bot(Configuration.BotInfo config, string apiKey, UserHandlerCreator handlerCreator, bool debug = false, bool process = false)
        {
            userHandlers = new Dictionary<SteamID, UserHandler>();
            logOnDetails = new SteamUser.LogOnDetails
            {
                Username = config.Username,
                Password = config.Password
            };
            DisplayName  = config.DisplayName;
            ChatResponse = config.ChatResponse;
            TradeOfferRefreshRate = config.TradeOfferRefreshRate;
            DisplayNamePrefix = config.DisplayNamePrefix;
            Admins = config.Admins;
            ApiKey = !String.IsNullOrEmpty(config.ApiKey) ? config.ApiKey : apiKey;
            isProccess = process;
            try
            {
                if( config.LogLevel != null )
                {
                    consoleLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.LogLevel, true);
                    Console.WriteLine(@"(Console) LogLevel configuration parameter used in bot {0} is depreciated and may be removed in future versions. Please use ConsoleLogLevel instead.", DisplayName);
                }
                else consoleLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.ConsoleLogLevel, true);
            }
            catch (ArgumentException)
            {
                Console.WriteLine(@"(Console) ConsoleLogLevel invalid or unspecified for bot {0}. Defaulting to ""Info""", DisplayName);
                consoleLogLevel = Log.LogLevel.Info;
            }

            try
            {
                fileLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.FileLogLevel, true);
            }
            catch (ArgumentException)
            {
                Console.WriteLine(@"(Console) FileLogLevel invalid or unspecified for bot {0}. Defaulting to ""Info""", DisplayName);
                fileLogLevel = Log.LogLevel.Info;
            }

            logFile = config.LogFile;
            CreateLog();
            createHandler = handlerCreator;
            BotControlClass = config.BotControlClass;
            SteamWeb = new SteamAPI.SteamWeb();

            // Hacking around https
            ServicePointManager.ServerCertificateValidationCallback += SteamWeb.ValidateRemoteCertificate;

            Log.Debug ("Initializing Steam Bot...");

            var mobileAuthCode = DoMobileAuth();
            if (!string.IsNullOrEmpty(mobileAuthCode))
            {
                logOnDetails.TwoFactorCode = mobileAuthCode;
            }

            SteamClient = new SteamClient();
            SteamClient.AddHandler(new SteamNotifications());
            SteamTrade = SteamClient.GetHandler<SteamTrading>();
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();
            SteamGameCoordinator = SteamClient.GetHandler<SteamGameCoordinator>();

            botThread = new BackgroundWorker { WorkerSupportsCancellation = true };
            botThread.DoWork += BackgroundWorkerOnDoWork;
            botThread.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;
            botThread.RunWorkerAsync();
        }

        ~Bot()
        {
            DisposeBot();
        }

        private void CreateLog()
        {
            if(Log == null)
                Log = new Log (logFile, DisplayName, consoleLogLevel, fileLogLevel);
        }

        private void DisposeLog()
        {
            if(Log != null)
            {
                Log.Dispose();
                Log = null;
            }
        }

        private void CreateFriendsListIfNecessary()
        {
            if (friends != null)
                return;

            friends = new List<SteamID>();
            for (int i = 0; i < SteamFriends.GetFriendCount(); i++)
                friends.Add(SteamFriends.GetFriendByIndex(i));
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
            CreateLog();
            IsRunning = true;

            Log.Info("Connecting...");
            if (!botThread.IsBusy)
                botThread.RunWorkerAsync();
            SteamClient.Connect();
            Log.Success("Done Loading Bot!");
            return true; // never get here
        }

        /// <summary>
        /// Disconnect from the Steam network and stop the callback
        /// thread.
        /// </summary>
        public void StopBot()
        {
            IsRunning = false;
            Log.Debug("Trying to shut down bot thread.");
            SteamClient.Disconnect();
            botThread.CancelAsync();
            userHandlers.Clear();
            DisposeLog();
        }

        public void HandleBotCommand(string command)
        {
            try
            {
                GetUserHandler(SteamClient.SteamID).OnBotCommand(command);
            }
            catch (ObjectDisposedException e)
            {
                // Writing to console because odds are the error was caused by a disposed Log.
                Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
                if (!this.IsRunning)
                {
                    Console.WriteLine("The Bot is no longer running and could not write to the Log. Try Starting this bot first.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
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

        void HandleSteamMessage(ICallbackMsg msg)
        {
            Log.Debug(msg.ToString());
            #region Login
            msg.Handle<SteamClient.ConnectedCallback> (callback =>
            {
                Log.Debug ("Connection Callback: {0}", callback.Result);

                if (callback.Result == EResult.OK)
                {
                    UserLogOn();
                }
                else
                {
                    Log.Error ("Failed to connect to Steam Community, trying again...");
                    SteamClient.Connect ();
                }

            });

            msg.Handle<SteamUser.LoggedOnCallback> (callback =>
            {
                Log.Debug("Logged On Callback: {0}", callback.Result);

                if (callback.Result == EResult.OK)
                {
                    myUserNonce = callback.WebAPIUserNonce;
                }
                else
                {
                    Log.Error("Login Error: {0}", callback.Result);
                }

                if (callback.Result == EResult.AccountLogonDenied)
                {
                    Log.Interface ("This account is SteamGuard enabled. Enter the code via the `auth' command.");

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
                    Log.Interface("The given SteamGuard code was invalid. Try again using the `auth' command.");
                    logOnDetails.AuthCode = Console.ReadLine();
                }                
            });

            msg.Handle<SteamUser.LoginKeyCallback> (callback =>
            {
                myUniqueId = callback.UniqueID.ToString();

                UserWebLogOn();

                SteamFriends.SetPersonaName (DisplayNamePrefix+DisplayName);
                SteamFriends.SetPersonaState (EPersonaState.Online);

                Log.Success ("Steam Bot Logged In Completely!");

                GetUserHandler(SteamClient.SteamID).OnLoginCompleted();
            });

            msg.Handle<SteamUser.WebAPIUserNonceCallback>(webCallback =>
            {
                Log.Debug("Received new WebAPIUserNonce.");

                if (webCallback.Result == EResult.OK)
                {
                    myUserNonce = webCallback.Nonce;
                    UserWebLogOn();
                }
                else
                {
                    Log.Error("WebAPIUserNonce Error: " + webCallback.Result);
                }
            });

            msg.Handle<SteamUser.UpdateMachineAuthCallback>(
                authCallback => OnUpdateMachineAuthCallback(authCallback)
            );
            #endregion

            #region Friends
            msg.Handle<SteamFriends.FriendsListCallback>(callback =>
            {
                foreach (SteamFriends.FriendsListCallback.Friend friend in callback.FriendList)
                {
                    switch (friend.SteamID.AccountType)
                    {
                        case EAccountType.Clan:
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
                            break;
                        default:
                            CreateFriendsListIfNecessary();
                            if (friend.Relationship == EFriendRelationship.None)
                            {
                                friends.Remove(friend.SteamID);
                                GetUserHandler(friend.SteamID).OnFriendRemove();
                                RemoveUserHandler(friend.SteamID);
                            }
                            else if (friend.Relationship == EFriendRelationship.RequestRecipient)
                    {
                                if (GetUserHandler(friend.SteamID).OnFriendAdd())
                                {
                        if (!friends.Contains(friend.SteamID))
                        {
                            friends.Add(friend.SteamID);
                                    }
                                    else
                            {
                                        Log.Error("Friend was added who was already in friends list: " + friend.SteamID);
                                    }
                                SteamFriends.AddFriend(friend.SteamID);
                            }
                        else
                        {
                                    SteamFriends.RemoveFriend(friend.SteamID);
                                RemoveUserHandler(friend.SteamID);
                            }
                        }
                            break;
                    }
                }
            });


            msg.Handle<SteamFriends.FriendMsgCallback> (callback =>
            {
                EChatEntryType type = callback.EntryType;

                if (callback.EntryType == EChatEntryType.ChatMsg)
                {
                    Log.Info ("Chat Message from {0}: {1}",
                                         SteamFriends.GetFriendPersonaName (callback.Sender),
                                         callback.Message
                                         );
                    GetUserHandler(callback.Sender).OnMessageHandler(callback.Message, type);
                }
            });
            #endregion

            #region Group Chat
            msg.Handle<SteamFriends.ChatMsgCallback>(callback =>
            {
                GetUserHandler(callback.ChatterID).OnChatRoomMessage(callback.ChatRoomID, callback.ChatterID, callback.Message);
            });
            #endregion

            #region Disconnect
            msg.Handle<SteamUser.LoggedOffCallback> (callback =>
            {
                IsLoggedIn = false;
                Log.Warn("Logged off Steam.  Reason: {0}", callback.Result);
            });

            msg.Handle<SteamClient.DisconnectedCallback> (callback =>
            {
                if(IsLoggedIn)
                {
                    IsLoggedIn = false;
                    Log.Warn("Disconnected from Steam Network!");
                }

                SteamClient.Connect ();
            });
            #endregion
        }

        string DoMobileAuth()
        {
            var authFile = Path.Combine("authfiles", String.Format("{0}.auth", logOnDetails.Username));
            if (File.Exists(authFile))
            {
                SteamGuardAccount = Newtonsoft.Json.JsonConvert.DeserializeObject<SteamGuardAccount>(File.ReadAllText(authFile));                
                return SteamGuardAccount.GenerateSteamGuardCode();
            }
            else
            {
                var login = new UserLogin(logOnDetails.Username, logOnDetails.Password);
                var loginResult = login.DoLogin();
                if (loginResult == LoginResult.NeedEmail)
                {
                    while (loginResult == LoginResult.NeedEmail)
                    {
                        Console.WriteLine("Enter Email code:");
                        var emailCode = Console.ReadLine();
                        login.EmailCode = emailCode;
                        loginResult = login.DoLogin();
                    }                    
                }
                if (loginResult == LoginResult.LoginOkay)
                {
                    Log.Info("Linking mobile authenticator...");
                    var authLinker = new AuthenticatorLinker(login.Session);
                    var addAuthResult = authLinker.AddAuthenticator();
                    if (addAuthResult == AuthenticatorLinker.LinkResult.MustProvidePhoneNumber)
                    {
                        while (addAuthResult == AuthenticatorLinker.LinkResult.MustProvidePhoneNumber)
                        {
                            Console.WriteLine("Enter phone number (must provide country code, e.g. +1XXXXXXXXXXX):");
                            var phoneNumber = Console.ReadLine();
                            authLinker.PhoneNumber = phoneNumber;
                            addAuthResult = authLinker.AddAuthenticator();
                        }
                    }
                    if (addAuthResult == AuthenticatorLinker.LinkResult.AwaitingFinalization)
                    {
                        SteamGuardAccount = authLinker.LinkedAccount;
                        try
                        {
                            Directory.CreateDirectory(Path.Combine(System.Windows.Forms.Application.StartupPath, "authfiles"));
                            File.WriteAllText(authFile, Newtonsoft.Json.JsonConvert.SerializeObject(SteamGuardAccount));
                        }
                        catch
                        {

                        }
                        Console.WriteLine("Enter SMS code:");
                        var smsCode = Console.ReadLine();
                        var authResult = authLinker.FinalizeAddAuthenticator(smsCode);
                        if (authResult == AuthenticatorLinker.FinalizeResult.Success)
                        {
                            Log.Success("Linked authenticator.");                            
                            return SteamGuardAccount.GenerateSteamGuardCode();
                        }
                        else
                        {
                            Log.Error("Error linking authenticator: " + authResult);
                        }
                    }
                    else
                    {
                        Log.Error("Error adding authenticator: " + addAuthResult);
                    }                
                }
                else
                {
                    Log.Error("Error performing mobile login: " + loginResult);
                }
            }
            return string.Empty;          
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

        void UserWebLogOn()
        {
            do
            {
                IsLoggedIn = SteamWeb.Authenticate(myUniqueId, SteamClient, myUserNonce);

                if(!IsLoggedIn)
                {
                    Log.Warn("Authentication failed, retrying in 2s...");
                    Thread.Sleep(2000);
                }
            } while(!IsLoggedIn);

            Log.Success("User Authenticated!");
            
            cookiesAreInvalid = false;

            TradeOffers = new TradeOffers(SteamUser.SteamID, SteamWeb, ApiKey, TradeOfferRefreshRate);
            TradeOffers.TradeOfferAccepted += TradeOffers_TradeOfferAccepted;
            TradeOffers.TradeOfferDeclined += TradeOffers_TradeOfferDeclined;
            TradeOffers.TradeOfferReceived += TradeOffers_TradeOfferReceived;
            TradeOffers.TradeOfferInvalid += TradeOffers_TradeOfferInvalid;
            TradeOffers.TradeOfferNeedsConfirmation += TradeOffers_TradeOfferNeedsConfirmation;

            GetUserHandler(SteamClient.SteamID).OnLoginCompleted();
        }

        private void TradeOffers_TradeOfferNeedsConfirmation(object sender, TradeOffers.TradeOfferEventArgs e)
        {
            GetUserHandler(e.TradeOffer.OtherSteamId).OnTradeOfferNeedsConfirmation(e.TradeOffer);
        }

        private void TradeOffers_TradeOfferInvalid(object sender, TradeOffers.TradeOfferEventArgs e)
        {
            GetUserHandler(e.TradeOffer.OtherSteamId).OnTradeOfferInvalid(e.TradeOffer);
        }

        void TradeOffers_TradeOfferReceived(object sender, TradeOffers.TradeOfferEventArgs e)
        {
            GetUserHandler(e.TradeOffer.OtherSteamId).OnTradeOfferReceived(e.TradeOffer);
        }

        void TradeOffers_TradeOfferDeclined(object sender, TradeOffers.TradeOfferEventArgs e)
        {
            GetUserHandler(e.TradeOffer.OtherSteamId).OnTradeOfferDeclined(e.TradeOffer);
        }

        void TradeOffers_TradeOfferAccepted(object sender, TradeOffers.TradeOfferEventArgs e)
        {
            GetUserHandler(e.TradeOffer.OtherSteamId).OnTradeOfferAccepted(e.TradeOffer);
        }

        /// <summary>
        /// Checks if sessionId and token cookies are still valid.
        /// Sets cookie flag if they are invalid.
        /// </summary>
        /// <returns>true if cookies are valid; otherwise false</returns>
        bool CheckCookies()
        {
            // We still haven't re-authenticated
            if (cookiesAreInvalid)
                return false;

            try
            {
                if (!SteamWeb.VerifyCookies())
                {
                    // Cookies are no longer valid
                    Log.Warn("Cookies are invalid. Need to re-authenticate.");
                    cookiesAreInvalid = true;
                    SteamUser.RequestWebAPIUserNonce();
                    return false;
                }
            }
            catch
            {
                // Even if exception is caught, we should still continue.
                Log.Warn("Cookie check failed. http://steamcommunity.com is possibly down.");
            }

            return true;
        }

        UserHandler GetUserHandler(SteamID sid)
        {
            if (!userHandlers.ContainsKey(sid))
                userHandlers[sid] = createHandler(this, sid);
            return userHandlers[sid];
        }

        void RemoveUserHandler(SteamID sid)
        {
            if (userHandlers.ContainsKey(sid))
                userHandlers.Remove(sid);
        }

        static byte [] SHAHash (byte[] input)
        {
            SHA1Managed sha = new SHA1Managed();
            
            byte[] output = sha.ComputeHash( input );
            
            sha.Clear();
            
            return output;
        }

        void OnUpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth)
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
                JobID = machineAuth.JobID, // so we respond to the correct server job
            };
            
            // send off our response
            SteamUser.SendMachineAuthResponse (authResponse);
        }
        
        #region Background Worker Methods

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            if (runWorkerCompletedEventArgs.Error != null)
            {
                Exception ex = runWorkerCompletedEventArgs.Error;

                Log.Error("Unhandled exceptions in bot {0} callback thread: {1} {2}",
                      DisplayName,
                      Environment.NewLine,
                      ex);

                Log.Info("This bot died. Stopping it..");
                //backgroundWorker.RunWorkerAsync();
                //Thread.Sleep(10000);
                StopBot();
                //StartBot();
            }

            Log.Dispose();
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            ICallbackMsg msg;

            while (!botThread.CancellationPending)
            {
                try
                {
                    msg = SteamClient.WaitForCallback(true);
                    HandleSteamMessage(msg);
                }
                catch (WebException e)
                {
                    Log.Error("URI: {0} >> {1}", (e.Response != null && e.Response.ResponseUri != null ? e.Response.ResponseUri.ToString() : "unknown"), e.ToString());
                    System.Threading.Thread.Sleep(45000);//Steam is down, retry in 45 seconds.
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    Log.Warn("Restarting bot...");
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

        public void Dispose()
        {
            DisposeBot();
            GC.SuppressFinalize(this);
        }

        private void DisposeBot()
        {
            if (disposed)
                return;
            disposed = true;
            StopBot();
        }
    }
}
