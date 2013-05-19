using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.TF2;
using SteamKit2.Internal;

namespace SteamBot
{
    public class MsgCraft : IGCSerializableMessage
    {

        public ulong[] idsToSend;
        public short recipe = -2;
        public short numItems = 2;

        public MsgCraft()
        {
        }

        public bool IsProto
        {
            get { return false; }
        }

        //1002 is the EMsg code for crafting
        public uint MsgType
        {
            get { return 1002; }
        }

        public JobID TargetJobID
        {
            get { return new JobID(); }
            set { throw new NotImplementedException(); }
        }

        public JobID SourceJobID
        {
            get { return new JobID(); }
            set { throw new NotImplementedException(); }
        }

        public byte[] Serialize()
        {
            List<byte> ret = new List<byte>();
            ret.AddRange(BitConverter.GetBytes((short)recipe));
            ret.AddRange(BitConverter.GetBytes((short)numItems));
            return ret.ToArray();
        }

        public void Serialize(Stream stream)
        {
            byte[] buf = Serialize();
            stream.Write(buf, 0, buf.Length);
        }

        public void Deserialize(Stream stream)
        {

        }


        //1002 is the EMsg code for crafting
        public uint GetEMsg()
        {
            return 1002;
        }
    }
}
