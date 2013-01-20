using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamBot.Trading;

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
        public abstract void OnClientConnected(SteamClient.ConnectedCallback callback);

        /// <summary>
        /// Handle the login for the Bot class.  This is called after logging 
        /// in.
        /// </summary>
        /// <param name="callback">The callback response.</param>
        public abstract void OnUserLoggedOn(SteamUser.LoggedOnCallback callback);

        /// <summary>
        /// Handle the login for the Bot class.  This is called when the user
        /// is compeletely logged in.
        /// </summary>
        /// <param name="callback">The callback response.</param>
        public abstract void OnUserLoginKey(SteamUser.LoginKeyCallback callback);

        /// <summary>
        /// Handle shutting down the connection.  Take as much time as you
        /// need.
        /// </summary>
        public abstract void OnBotShutdown();

        /// <summary>
        /// When the bot is disconnected from Steam.
        /// </summary>
        public abstract void OnClientDisconnect(SteamClient.DisconnectedCallback callback);

        /// <summary>
        /// When the bot is logged off.
        /// </summary>
        /// <param name="callback">The callback response.</param>
        public abstract void OnUserLoggedOff(SteamUser.LoggedOffCallback callback);

        /// <summary>
        /// This is called when a friend added the bot.
        /// </summary>
        /// <param name="steamId">The steam ID of the friend.</param>
        public abstract void OnFriendAdd(SteamID steamId);

        /// <summary>
        /// This is called when a friend messages the bot.
        /// </summary>
        /// <param name="callback"></param>
        public abstract void OnFriendMsg(SteamFriends.FriendMsgCallback callback);

        /// <summary>
        /// This is called when the bot recieves their friends list from the
        /// server.  This includes friends whose relationship status is still
        /// not 'friend'.
        /// </summary>
        /// <param name="callback"></param>
        public virtual void OnFriendsListUpdate(SteamFriends.FriendsListCallback callback) { }

        /// <summary>
        /// This is called when the bot recieves a persona change from a 
        /// friend.
        /// </summary>
        /// <param name="callback"></param>
        public virtual void OnFriendsPersonaStateUpdate(SteamFriends.PersonaStateCallback callback) { }

        public abstract void OnTradeProposed(SteamTrading.TradeProposedCallback callback);

        public abstract void OnTradeResult(SteamTrading.TradeResultCallback callback);

        public abstract void OnTradingSessionStart(SteamTrading.SessionStartCallback callback);

        public abstract void HandleTradeClose(Api.ETradeStatus status);

        public Bot bot;
        public SteamClient steamClient;
        public SteamUser steamUser;
        public SteamFriends steamFriends;
        public SteamTrading steamTrading;
        public SteamUser.LogOnDetails logOnDetails;
        public CallbackManager manager;
        public string sessionId;
        public string steamLogin;
        public Trading.Web web;
    }
}
