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

        /// <summary>
        /// This initializes the bot runner.
        /// </summary>
        /// <param name="options">The line options passed to the bot.</param>
        void Start(Options options);

        /// <summary>
        /// Log to whatever.
        /// </summary>
        /// <param name="type">The type of log.</param>
        /// <param name="log">The log string to log.</param>
        void DoLog(ELogType type, string log);

        /// <summary>
        /// Log to whatever, within a namespace.
        /// </summary>
        /// <param name="type">The type of log.</param>
        /// <param name="name">The namespace to log under.</param>
        /// <param name="log">The log string to log.</param>
        void DoLog(ELogType type, string name, string log);

        /// <summary>
        /// Gets the SteamGuard code.
        /// </summary>
        /// <returns>The steamguard code.</returns>
        string GetSteamGuardCode();
    }
}

