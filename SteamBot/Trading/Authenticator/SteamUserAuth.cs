using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using SteamKit2;

namespace SteamBot.Trading.Authenticator
{
    class SteamUserAuth : IAuthenticator
    {
        public Web web { get; set; }
        public SteamUser.LoginKeyCallback loginKeyCallback { get; set; }
        public BotHandler handler { get; set; }

        public string[] Authenticate()
        {
            string sessionId = Convert.ToBase64String(Encoding.UTF8.GetBytes(loginKeyCallback.UniqueID.ToString()));
            string token = "";
            using (WebAPI.Interface userAuth = WebAPI.GetInterface("ISteamUserAuth"))
            {
                // generate an AES session key
                var sessionKey = CryptoHelper.GenerateRandomBlock(32);

                // rsa encrypt it with the public key for the universe we're on
                byte[] cryptedSessionKey = null;
                using (RSACrypto rsa = new RSACrypto(KeyDictionary.GetPublicKey(handler.steamClient.ConnectedUniverse)))
                {
                    cryptedSessionKey = rsa.Encrypt(sessionKey);
                }


                byte[] loginKey = new byte[20];
                Array.Copy(Encoding.ASCII.GetBytes(loginKeyCallback.LoginKey), loginKey, loginKeyCallback.LoginKey.Length);

                // aes encrypt the loginkey with our session key
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

                KeyValue authResult;

                Dictionary<String, String> args = new Dictionary<string, string>();
                args.Add("steamid", handler.steamClient.SteamID.ConvertToUInt64().ToString());
                args.Add("sessionkey", HttpUtility.UrlEncode(cryptedSessionKey));
                args.Add("encrypted_loginkey", HttpUtility.UrlEncode(cryptedLoginKey));

                try
                {
                    authResult = userAuth.Call("AuthenticateUser", 1, args, "POST", true);
                    token = authResult["token"].AsString();
                }
                catch (Exception)
                {
                    token = "";
                }
            }
            return new string[] { sessionId, token };
        }
    }
}
