namespace SteamBot
{
    public interface ILog : System.IDisposable
    {
        bool ShowBotName { get; set; }

        void Debug(string data, params object[] formatParams);
        void Error(string data, params object[] formatParams);
        void Info(string data, params object[] formatParams);
        void Interface(string data, params object[] formatParams);
        void Success(string data, params object[] formatParams);
        void Warn(string data, params object[] formatParams);
    }
}