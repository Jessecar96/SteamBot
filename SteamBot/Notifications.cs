using SteamKit2;
using SteamKit2.Internal;
using System.Collections.ObjectModel;
using System.Linq;

namespace SteamBot
{
    public class SteamNotifications : ClientMsgHandler
    {
        public class NotificationCallback : CallbackMsg
        {
            public ReadOnlyCollection<Notification> Notifications { get; private set; }

            internal NotificationCallback(CMsgClientUserNotifications msg)
            {
                var list = msg.notifications
                    .Select(n => new Notification(n))
                    .ToList();

                this.Notifications = new ReadOnlyCollection<Notification>(list);
            }

            public sealed class Notification
            {
                internal Notification(CMsgClientUserNotifications.Notification notification)
                {
                    Count = notification.count;
                    UserNotificationType = (UserNotificationType)notification.user_notification_type;
                }

                public uint Count { get; private set; }

                public UserNotificationType UserNotificationType { get; private set; }
            }

            public enum UserNotificationType
            {
                TradeOffer = 1,
                Unknown
            }
        }

        public class CommentNotificationCallback : CallbackMsg
        {
            public CommentNotification CommentNotifications { get; private set; }

            internal CommentNotificationCallback(CMsgClientCommentNotifications msg)
            {
                CommentNotifications = new CommentNotification(msg);
            }

            public sealed class CommentNotification
            {
                internal CommentNotification(CMsgClientCommentNotifications msg)
                {
                    CountNewComments = msg.count_new_comments;
                    CountNewCommentsOwner = msg.count_new_comments_owner;
                    CountNewCommentsSubscriptions = msg.count_new_comments_subscriptions;
                }

                public uint CountNewComments { get; private set; }

                public uint CountNewCommentsOwner { get; private set; }

                public uint CountNewCommentsSubscriptions { get; private set; }
            }
        }

        /// <summary>
        /// Request to see if the client user has any comment notifications
        /// </summary>
        public void RequestCommentNotifications()
        {
            var clientRequestCommentNotifications =
                new ClientMsgProtobuf<CMsgClientRequestCommentNotifications>(EMsg.ClientRequestCommentNotifications);

            Client.Send(clientRequestCommentNotifications);
        }

        /// <summary>
        /// Request to see if the client user has any notifications.
        /// </summary>
        public void RequestNotifications()
        {
            var requestItemAnnouncements =
                new ClientMsgProtobuf<CMsgClientRequestItemAnnouncements>(EMsg.ClientRequestItemAnnouncements);
            Client.Send(requestItemAnnouncements);
        }

        public override void HandleMsg(IPacketMsg packetMsg)
        {
            switch (packetMsg.MsgType)
            {
                case EMsg.ClientNewLoginKey:
                    HandleClientNewLoginKey(packetMsg);
                    break;

                case EMsg.ClientUserNotifications:
                    HandleClientUserNotifications(packetMsg);
                    break;

                case EMsg.ClientCommentNotifications:
                    HandleClientCommentNotifications(packetMsg);
                    break;
            }
        }

        private void HandleClientUserNotifications(IPacketMsg packetMsg)
        {
            var clientUserNotificationResponse = new ClientMsgProtobuf<CMsgClientUserNotifications>(packetMsg);

            CMsgClientUserNotifications result = clientUserNotificationResponse.Body;

            Client.PostCallback(new NotificationCallback(result));
        }

        private void HandleClientCommentNotifications(IPacketMsg packetMsg)
        {
            var clientCommentNotifications = new ClientMsgProtobuf<CMsgClientCommentNotifications>(packetMsg);

            CMsgClientCommentNotifications result = clientCommentNotifications.Body;

            Client.PostCallback(new CommentNotificationCallback(result));
        }

        private void HandleClientNewLoginKey(IPacketMsg packetMsg)
        {
            this.RequestCommentNotifications();
            this.RequestNotifications();
        }
    }
}