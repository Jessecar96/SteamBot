using System;
using System.Collections.Generic;
using System.IO;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamBot.TF2GC
{
    public sealed class MsgDelete : IGCSerializableMessage
    {
        public MsgDelete() { }

        public bool IsProto { get { return false; } }

        public uint MsgType { get { return 1004; } }

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
            return ret.ToArray();
        }

        public void Serialize(Stream stream)
        {
            byte[] buf = Serialize();
            stream.Write(buf, 0, buf.Length);
        }

        public void Deserialize(Stream stream) { }

        public uint GetEMsg() { return 1004; }
    }
}