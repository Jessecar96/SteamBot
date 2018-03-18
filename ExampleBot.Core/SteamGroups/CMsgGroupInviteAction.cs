///SteamKit2.ClanMessages
///Created by Jacob Douglas (gamemaster1494)
///Created on 4/17/2013 at 2:19 AM CST
///(c) Copyright 2013, Nebula Programs

/// Purpose: This class contains the messages used to Invite friends to clans
/// As well as Decline or Accept clan invites.

using System.IO;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamBot.SteamGroups
{
    //CMsgInviteUserToClan

    /// <summary>
    /// Message used to Accept or Decline a group(clan) invite.
    /// </summary>
    public class CMsgGroupInviteAction : ISteamSerializableMessage, ISteamSerializable
    {
        EMsg ISteamSerializableMessage.GetEMsg()
        {
            return EMsg.ClientAcknowledgeClanInvite;
        }

        public CMsgGroupInviteAction()
        {

        }

        /// <summary>
        /// Group invited to.
        /// </summary>
        public ulong GroupID = 0;

        /// <summary>
        /// To accept or decline the invite.
        /// </summary>
        public bool AcceptInvite = true;

        void ISteamSerializable.Serialize(Stream stream)
        {
            try
            {
                BinaryWriter bw = new BinaryWriter(stream);
                bw.Write(GroupID);
                bw.Write(AcceptInvite);
            }//try
            catch
            {
                throw new IOException();
            }//catch
        }//Serialize()

        void ISteamSerializable.Deserialize(Stream stream)
        {
            try
            {
                BinaryReader br = new BinaryReader(stream);
                GroupID = br.ReadUInt64();
                AcceptInvite = br.ReadBoolean();
            }//try
            catch
            {
                throw new IOException();
            }//catch
        }//Deserialize()
    }//CMsgClanInviteAction

}
