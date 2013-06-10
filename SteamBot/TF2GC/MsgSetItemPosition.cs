using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.TF2;
using SteamKit2.Internal;

namespace SteamBot.TF2GC
{
    public class MsgSetItemPosition : IGCSerializableMessage
    {

        public MsgSetItemPosition()
        {

        }

        public bool IsProto
        {
            get { return false; }
        }

        public uint MsgType
        {
            get { return 1001; }
        }

        public JobID TargetJobID
        {
            get { return new JobID (); }
            set { throw new NotImplementedException (); }
        }

        public JobID SourceJobID
        {
            get { return new JobID (); }
            set { throw new NotImplementedException (); }
        }

        public byte[] Serialize()
        {
            List<byte> ret = new List<byte> ();
            return ret.ToArray ();
        }

        public void Serialize(Stream stream)
        {
            byte[] buf = Serialize ();
            stream.Write (buf, 0, buf.Length);
        }

        public void Deserialize(Stream stream)
        {

        }

        public uint GetEMsg()
        {
            //1001 is the EMsg for item position (see SteamKit2's enum somewhere)
            return 1001;
        }
    }
}