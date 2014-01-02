using System;
using System.IO;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using SteamKit2;

namespace SteamTrade
{
    public class SteamWeb
    {
        public static string Fetch (string url, string method, NameValueCollection data = null, CookieContainer cookies = null, bool ajax = true)
        {
            HttpWebResponse response = Request (url, method, data, cookies, ajax);
            using(Stream responseStream = response.GetResponseStream())
            {
                using(StreamReader reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static HttpWebResponse Request (string url, string method, NameValueCollection data = null, CookieContainer cookies = null, bool ajax = true)
        {
            HttpWebRequest request = WebRequest.Create (url) as HttpWebRequest;

            request.Method = method;
            request.Accept = "application/json, text/javascript;q=0.9, */*;q=0.5";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //request.Host is set automatically
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
            request.Referer = "http://steamcommunity.com/trade/1";
            request.Timeout = 50000; //Timeout after 50 seconds

            if (ajax)
            {
                request.Headers.Add ("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add ("X-Prototype-Version", "1.7");
            }

            // Cookies
            request.CookieContainer = cookies ?? new CookieContainer ();

            // Request data
            if (data != null)
            {
                string dataString = String.Join ("&", Array.ConvertAll (data.AllKeys, key =>
                    String.Format ("{0}={1}", HttpUtility.UrlEncode (key), HttpUtility.UrlEncode (data [key]))
                ));

                byte[] dataBytes = Encoding.UTF8.GetBytes (dataString);
                request.ContentLength = dataBytes.Length;

                using(Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(dataBytes, 0, dataBytes.Length);
                }
            }

            // Get the response
            return request.GetResponse () as HttpWebResponse;
        }

        /// <summary>
        /// Executes the login by using the Steam Website.
        /// </summary>
        public static CookieCollection DoLogin (string username, string password)
        {
            var data = new NameValueCollection ();
            data.Add ("username", username);
            string response = Fetch ("https://steamcommunity.com/login/getrsakey", "POST", data, null, false);
            GetRsaKey rsaJSON = JsonConvert.DeserializeObject<GetRsaKey> (response);


            // Validate
            if (rsaJSON.success != true)
            {
                return null;
            }

            //RSA Encryption
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider ();
            RSAParameters rsaParameters = new RSAParameters ();

            rsaParameters.Exponent = HexToByte (rsaJSON.publickey_exp);
            rsaParameters.Modulus = HexToByte (rsaJSON.publickey_mod);

            rsa.ImportParameters (rsaParameters);

            byte[] bytePassword = Encoding.ASCII.GetBytes (password);
            byte[] encodedPassword = rsa.Encrypt (bytePassword, false);
            string encryptedBase64Password = Convert.ToBase64String (encodedPassword);


            SteamResult loginJson = null;
            CookieCollection cookies;
            string steamGuardText = "";
            string steamGuardId   = "";
            do
            {
                Console.WriteLine ("SteamWeb: Logging In...");

                bool captcha = loginJson != null && loginJson.captcha_needed == true;
                bool steamGuard = loginJson != null && loginJson.emailauth_needed == true;

                string time = Uri.EscapeDataString (rsaJSON.timestamp);
                string capGID = loginJson == null ? null : Uri.EscapeDataString (loginJson.captcha_gid);

                data = new NameValueCollection ();
                data.Add ("password", encryptedBase64Password);
                data.Add ("username", username);

                // Captcha
                string capText = "";
                if (captcha)
                {
                    Console.WriteLine ("SteamWeb: Captcha is needed.");
                    System.Diagnostics.Process.Start ("https://steamcommunity.com/public/captcha.php?gid=" + loginJson.captcha_gid);
                    Console.WriteLine ("SteamWeb: Type the captcha:");
                    capText = Uri.EscapeDataString (Console.ReadLine ());
                }

                data.Add ("captchagid", captcha ? capGID : "");
                data.Add ("captcha_text", captcha ? capText : "");
                // Captcha end

                // SteamGuard
                if (steamGuard)
                {
                    Console.WriteLine ("SteamWeb: SteamGuard is needed.");
                    Console.WriteLine ("SteamWeb: Type the code:");
                    steamGuardText = Uri.EscapeDataString (Console.ReadLine ());
                    steamGuardId   = loginJson.emailsteamid;
                }

                data.Add ("emailauth", steamGuardText);
                data.Add ("emailsteamid", steamGuardId);
                // SteamGuard end

                data.Add ("rsatimestamp", time);

                HttpWebResponse webResponse = Request ("https://steamcommunity.com/login/dologin/", "POST", data, null, false);

                StreamReader reader = new StreamReader (webResponse.GetResponseStream ());
                string json = reader.ReadToEnd ();

                loginJson = JsonConvert.DeserializeObject<SteamResult> (json);

                cookies = webResponse.Cookies;
            } while (loginJson.captcha_needed == true || loginJson.emailauth_needed == true);


            if (loginJson.success == true)
            {
                CookieContainer c = new CookieContainer ();
                foreach (Cookie cookie in cookies)
                {
                    c.Add (cookie);
                }
                SubmitCookies (c);
                return cookies;
            }
            else
            {
                Console.WriteLine ("SteamWeb Error: " + loginJson.message);
                return null;
            }

        }

        ///<summary>
        /// Authenticate using SteamKit2 and ISteamUserAuth. 
        /// This does the same as SteamWeb.DoLogin(), but without contacting the Steam Website.
        /// </summary> 
        /// <remarks>Should this one doesnt work anymore, use <see cref="SteamWeb.DoLogin"/></remarks>
        public static bool Authenticate(SteamUser.LoginKeyCallback callback, SteamClient client, out string sessionId, out string token, string MyLoginKey)
        {
            sessionId = Convert.ToBase64String (Encoding.UTF8.GetBytes (callback.UniqueID.ToString ()));
            
            using (dynamic userAuth = WebAPI.GetInterface ("ISteamUserAuth"))
            {
                // generate an AES session key
                var sessionKey = CryptoHelper.GenerateRandomBlock (32);
                
                // rsa encrypt it with the public key for the universe we're on
                byte[] cryptedSessionKey = null;
                using (RSACrypto rsa = new RSACrypto (KeyDictionary.GetPublicKey (client.ConnectedUniverse)))
                {
                    cryptedSessionKey = rsa.Encrypt (sessionKey);
                }
                
                
                byte[] loginKey = new byte[20];
                Array.Copy(Encoding.ASCII.GetBytes(MyLoginKey), loginKey, MyLoginKey.Length);
                
                // aes encrypt the loginkey with our session key
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt (loginKey, sessionKey);
                
                KeyValue authResult;
                
                try
                {
                    authResult = userAuth.AuthenticateUser (
                        steamid: client.SteamID.ConvertToUInt64 (),
                        sessionkey: HttpUtility.UrlEncode (cryptedSessionKey),
                        encrypted_loginkey: HttpUtility.UrlEncode (cryptedLoginKey),
                        method: "POST"
                        );
                }
                catch (Exception)
                {
                    token = null;
                    return false;
                }
                
                token = authResult ["token"].AsString ();
                
                return true;
            }
        }

        static void SubmitCookies (CookieContainer cookies)
        {
            HttpWebRequest w = WebRequest.Create ("https://steamcommunity.com/") as HttpWebRequest;

            w.Method = "POST";
            w.ContentType = "application/x-www-form-urlencoded";
            w.CookieContainer = cookies;

            w.GetResponse ().Close ();
            return;
        }

        static byte[] HexToByte (string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception ("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];
            int l = hex.Length;

            for (int i = 0; i < (l >> 1); ++i)
            {
                arr [i] = (byte)((GetHexVal (hex [i << 1]) << 4) + (GetHexVal (hex [(i << 1) + 1])));
            }

            return arr;
        }

        static int GetHexVal (char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : 55);
        }

        public static bool ValidateRemoteCertificate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // allow all certificates
            return true;
        }

    }

    // JSON Classes
    public class GetRsaKey
    {
        public bool success { get; set; }

        public string publickey_mod { get; set; }

        public string publickey_exp { get; set; }

        public string timestamp { get; set; }
    }

    public class SteamResult
    {
        public bool success { get; set; }

        public string message { get; set; }

        public bool captcha_needed { get; set; }

        public string captcha_gid { get; set; }

        public bool emailauth_needed { get; set; }

        public string emailsteamid { get; set; }

    }

}
