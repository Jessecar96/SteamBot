using System;

namespace SteamBot
{
    public enum ELogType {
        DEBUG,
        INFO,
        SUCCESS,
        WARN,
        ERROR,
        INTERFACE,
        NOTHING
    }

    public interface IBotRunner
    {
        void Start(Options options);
        void DoLog(ELogType type, string log);
        void DoLog(ELogType type, string name, string log);
    }
}

