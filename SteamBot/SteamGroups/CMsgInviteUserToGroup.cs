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
    /// <summary>
    /// Message used to invite a user to a group(clan).
    /// </summary>
    public class CMsgInviteUserToGroup : ISteamSerializableMessage, ISteamSerializable
    {
        EMsg ISteamSerializableMessage.GetEMsg()
        {
            return EMsg.ClientInviteUserToClan;
        }

        public CMsgInviteUserToGroup()
        {

        }

        /// <summary>
        /// Who is being invited.
        /// </summary>
        public ulong Invitee = 0;

        /// <summary>
        /// Group to invite to
        /// </summary>
        public ulong GroupID = 0;

        /// <summary>
        /// Not known yet. All data seen shows this as being true.
        /// See what happens if its false? 
        /// </summary>
        public bool UnknownInfo = true;

        void ISteamSerializable.Serialize(Stream stream)
        {
            try
            {
                BinaryWriter bw = new BinaryWriter(stream);
                bw.Write(Invitee);
                bw.Write(GroupID);
                bw.Write(UnknownInfo);
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
                Invitee = br.ReadUInt64();
                GroupID = br.ReadUInt64();
                UnknownInfo = br.ReadBoolean();
            }//try
            catch
            {
                throw new IOException();
            }//catch
        }//Deserialize()
    }
}