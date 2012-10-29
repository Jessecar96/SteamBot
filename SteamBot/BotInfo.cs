namespace SteamBot
{
    public class BotFile
    {
        public ulong[] Admins { get; set; }
        public BotInfo[] Bots { get; set; }
        public string ApiKey { get; set; }
    }

    public class BotInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public string ChatResponse { get; set; }
        public ulong[] Admins;
    }
}
