using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot
{
    public abstract class BotHandler
    {
        /// <summary>
        /// Handle the connection for the Bot class.
        /// </summary>
        public abstract void HandleBotConnection();

        /// <summary>
        /// Handle the login for the Bot class.  This is called after being 
        /// connected.
        /// </summary>
        /// <param name="callback">The callback response.</param>
        public abstract void HandleBotLogin(SteamClient.ConnectedCallback callback);

        /// <summary>
        /// Handle the login for the Bot class.  This is called after logging 
        /// in.
        /// </summary>
        /// <param name="callback">The callback response.</param>
        public abstract void HandleBotLogin(SteamUser.LoggedOnCallback callback);

        /// <summary>
        /// Handle the login for the Bot class.  This is called when the user
        /// is compeletely logged in.
        /// </summary>
        /// <param name="callback">The callback response.</param>
        public abstract void HandleBotLogin(SteamUser.LoginKeyCallback callback);

        /// <summary>
        /// Handle shutting down the connection.  Take as much time as you
        /// need.
        /// </summary>
        public abstract void HandleBotShutdown();

        /// <summary>
        /// When the bot is disconnected from Steam.
        /// </summary>
        public abstract void HandleBotDisconnect();

        /// <summary>
        /// When the bot is logged off.
        /// </summary>
        /// <param name="callback">The callback response.</param>
        public abstract void HandleBotLogoff(SteamUser.LoggedOffCallback callback);

        /// <summary>
        /// Handle updating the SentryFile.
        /// </summary>
        /// <param name="machineAuth">The callback data for MachineAuth.</param>
        /// <param name="jobId">The JobID.</param>
        public abstract void HandleUpdateMachineAuth(SteamUser.UpdateMachineAuthCallback machineAuth, JobID jobId);

        /// <summary>
        /// This is called when a friend added the bot.
        /// </summary>
        /// <param name="steamId">The steam ID of the friend.</param>
        public abstract void HandleFriendAdd(SteamID steamId);

        /// <summary>
        /// This is called when a friend messages the bot.
        /// </summary>
        /// <param name="callback"></param>
        public abstract void HandleFriendMsg(SteamFriends.FriendMsgCallback callback);

        /// <summary>
        /// This is called when the bot recieves their friends list from the
        /// server.  This includes friends whose relationship status is still
        /// not 'friend'.
        /// </summary>
        /// <param name="callback"></param>
        public virtual void HandleFriendsList(SteamFriends.FriendsListCallback callback) { }

        /// <summary>
        /// This is called when the bot recieves a persona change from a 
        /// friend.
        /// </summary>
        /// <param name="callback"></param>
        public virtual void HandleFriendPersonaState(SteamFriends.PersonaStateCallback callback) { }

        public Bot bot;
        public SteamClient steamClient;
        public SteamUser steamUser;
        public SteamFriends steamFriends;
        public SteamUser.LogOnDetails logOnDetails;

        public string steamID;
        public string steamLogin;
        public Trading.Web web;
    }
}
