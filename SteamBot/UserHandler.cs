﻿using System;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamTrade;
using System.Threading.Tasks;

namespace SteamBot
{
    /// <summary>
    /// The abstract base class for users of SteamBot that will allow a user
    /// to extend the functionality of the Bot.
    /// </summary>
    public abstract class UserHandler
    {
        protected Bot Bot;
        protected SteamID OtherSID;
        private bool _lastMessageWasFromTrade;
        private Task<Inventory> otherInventoryTask;

        public UserHandler (Bot bot, SteamID sid)
        {
            Bot = bot;
            OtherSID = sid;
            GetOtherInventory();
        }

        /// <summary>
        /// Gets the other's inventory and stores it in OtherInventory.
        /// </summary>
        /// <example> This sample shows how to find items in the other's inventory from a user handler.
        /// <code>
        /// GetInventory(); // Not necessary unless you know the user's inventory has changed
        /// foreach (var item in OtherInventory)
        /// {
        ///     if (item.Defindex == 5021)
        ///     {
        ///         // Bot has a key in its inventory
        ///     }
        /// }
        /// </code>
        /// </example>
        public void GetOtherInventory()
        {
            otherInventoryTask = Task.Factory.StartNew(() =>Inventory.FetchInventory(OtherSID, Bot.apiKey));
        }

        public Inventory OtherInventory
        {
            get
            {
                otherInventoryTask.Wait();
                return otherInventoryTask.Result;
            }
        }

        /// <summary>
        /// Gets the Bot's current trade.
        /// </summary>
        /// <value>
        /// The current trade.
        /// </value>
        public Trade Trade
        {
            get
            {
                return Bot.CurrentTrade; 
            }
        }
        
        /// <summary>
        /// Gets the log the bot uses for convenience.
        /// </summary>
        protected Log Log
        {
            get { return Bot.log; }
        }
        
        /// <summary>
        /// Gets a value indicating whether the other user is admin.
        /// </summary>
        /// <value>
        /// <c>true</c> if the other user is a configured admin; otherwise, <c>false</c>.
        /// </value>
        protected bool IsAdmin
        {
            get { return Bot.Admins.Contains (OtherSID); }
        }

        /// <summary>
        /// Called when the bot is invited to a Steam group
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public abstract bool OnGroupAdd();

        /// <summary>
        /// Called when the user adds the bot as a friend.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public abstract bool OnFriendAdd ();

        /// <summary>
        /// Called when the user removes the bot as a friend.
        /// </summary>
        public abstract void OnFriendRemove ();

        public void OnMessageHandler(string message, EChatEntryType type)
        {
            _lastMessageWasFromTrade = false;
            OnMessage(message, type);
        }

        /// <summary>
        /// Called whenever a message is sent to the bot.
        /// This is limited to regular and emote messages.
        /// </summary>
        protected abstract void OnMessage (string message, EChatEntryType type);

        /// <summary>
        /// Called when the bot is fully logged in.
        /// </summary>
        public abstract void OnLoginCompleted();
       
        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public abstract bool OnTradeRequest ();

        /// <summary>
        /// Called when a chat message is sent in a chatroom
        /// </summary>
        /// <param name="chatID">The SteamID of the group chat</param>
        /// <param name="sender">The SteamID of the sender</param>
        /// <param name="message">The message sent</param>
        public virtual void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {

        }

        /// <summary>
        /// Called when an 'exec' command is given via botmanager.
        /// </summary>
        /// <param name="command">The command message.</param>
        public virtual void OnBotCommand(string command)
        {

        }

        /// <summary>
        /// Called when user accepts or denies bot's trade request.
        /// </summary>
        /// <param name="accepted">True if user accepted bot's request, false if not.</param>
        /// <param name="response">String response of the callback.</param>
        public virtual void OnTradeRequestReply(bool accepted, string response)
        {

        }

        #region Trade events
        // see the various events in SteamTrade.Trade for descriptions of these handlers.

        public abstract void OnTradeError (string error);

        public abstract void OnTradeTimeout ();

        public abstract void OnTradeSuccess ();

        public virtual void OnTradeClose ()
        {
            Bot.CloseTrade ();
        }

        public abstract void OnTradeInit ();

        public abstract void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem);

        public abstract void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem);

        public void OnTradeMessageHandler(string message)
        {
            _lastMessageWasFromTrade = true;
            OnTradeMessage(message);
        }

        protected abstract void OnTradeMessage (string message);

        public void OnTradeReadyHandler(bool ready)
        {
            Trade.Poll();
            OnTradeReady(ready);
        }

        public abstract void OnTradeReady (bool ready);

        public void OnTradeAcceptHandler()
        {
            Trade.Poll();
            if (Trade.OtherIsReady && Trade.MeIsReady)
            {
                OnTradeAccept();
            }
        }

        public abstract void OnTradeAccept();

        #endregion Trade events

        /// <summary>
        /// A helper method for sending a chat message to the other user in the chat window (as opposed to the trade window)
        /// </summary>
        /// <param name="message">The message to send to the other user</param>
        /// <param name="delayMs">Optional.  The delay before sending the message, in milliseconds.</param>
        protected virtual async void SendChatMessage(string message, int delayMs = 0)
        {
            if(delayMs != 0)
            {
                await Task.Delay(delayMs);
            }

            try
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, message);
            }
            catch(Exception ex)
            {
                Log.Error("Error occurred in SendChatMessage().  Message: \"{0}\" \nException: {1} ", message, ex.ToString());
            }
        }

        /// <summary>
        /// A helper method for sending a chat message to the other user in the trade window.
        /// </summary>
        /// <param name="message">The message to send to the other user</param>
        /// <param name="delayMs">Optional.  The delay before sending the message, in milliseconds.</param>
        protected virtual async void SendTradeMessage(string message, int delayMs = 0)
        {
            if(delayMs != 0)
            {
                await Task.Delay(delayMs);
            }

            try
            {
                Trade.SendMessage(message);
            }
            catch(Exception ex)
            {
                Log.Error("Error occurred in SendTradeMessage().  Message: \"{0}\" \nException: {1} ", message, ex.ToString());
            }
        }

        /// <summary>
        /// Sends a message to the user in either the chat window or the trade window, depending on which screen
        /// the user sent a message from last.  Useful for responding to commands.
        /// </summary>
        /// <param name="message">The message to send to the other user</param>
        /// <param name="delayMs">Optional.  The delay before sending the message, in milliseconds.</param>
        protected virtual void SendReplyMessage(string message, int delayMs = 0)
        {
            if(_lastMessageWasFromTrade && Trade != null)
            {
                SendTradeMessage(message, delayMs);
            }
            else
            {
                SendChatMessage(message, delayMs);
            }
        }
    }
}
