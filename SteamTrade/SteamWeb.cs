using Newtonsoft.Json;
using SteamKit2;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SteamTrade
{
    public class SteamWeb
    {
        public class ResponseBase
        {
            /// <summary>
            /// Did valve say the request was successful?
            /// </summary>
            public bool success { get; set; }

            /// <summary>
            /// Error string.
            /// </summary>
            public string error { get; set; }
        }

        public readonly object FAKE_RESPONSE = Task.Factory.StartNew(() => JsonConvert.DeserializeObject("{\"success\":\"false\"}")).Result;
        public const string SteamCommunityDomain = "steamcommunity.com";
        public string Token { get; private set; }
        public string SessionId { get; private set; }
        public string TokenSecure { get; private set; }
        private CookieContainer _cookies = new CookieContainer();

        public async Task<T> FetchJson<T>(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "") where T : ResponseBase
        {
            string response = await Fetch(url, method, data, ajax, referer);
            return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<T>(response));
        }

        public async Task<string> Fetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "")
        {
            using (HttpWebResponse response = await Request(url, method, data, ajax, referer))
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
        }

        public async Task<HttpWebResponse> Request(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "")
        {
            //Append the data to the URL for GET-requests
            bool isGetMethod = (method.ToLower() == "get");
            string dataString = (data == null ? null : String.Join("&", Array.ConvertAll(data.AllKeys, key =>
                String.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
            )));

            if (isGetMethod && !String.IsNullOrEmpty(dataString))
            {
                url += (url.Contains("?") ? "&" : "?") + dataString;
            }

            //Setup the request
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = method;
            request.Accept = "application/json, text/javascript;q=0.9, */*;q=0.5";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //request.Host is set automatically
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
            request.Referer = string.IsNullOrEmpty(referer) ? "http://steamcommunity.com/trade/1" : referer;
            request.Timeout = 50000; //Timeout after 50 seconds
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            if (ajax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }

            // Cookies
            request.CookieContainer = _cookies;

            // Write the data to the body for POST and other methods
            if (!isGetMethod && !String.IsNullOrEmpty(dataString))
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
                request.ContentLength = dataBytes.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    await requestStream.WriteAsync(dataBytes, 0, dataBytes.Length);
                }
            }

            // Get the response
            return await request.GetResponseAsync() as HttpWebResponse;
        }

        /// <summary>
        /// Executes the login by using the Steam Website.
        /// </summary>
        public async Task<bool> DoLogin(string username, string password)
        {
            var data = new NameValueCollection();
            data.Add("username", username);
            GetRsaKey rsaJSON = await FetchJson<GetRsaKey>("https://steamcommunity.com/login/getrsakey", "POST", data, false);


            // Validate
            if (!rsaJSON.success)
            {
                return false;
            }

            //RSA Encryption
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            RSAParameters rsaParameters = new RSAParameters();

            rsaParameters.Exponent = HexToByte(rsaJSON.publickey_exp);
            rsaParameters.Modulus = HexToByte(rsaJSON.publickey_mod);

            rsa.ImportParameters(rsaParameters);

            byte[] bytePassword = Encoding.ASCII.GetBytes(password);
            byte[] encodedPassword = rsa.Encrypt(bytePassword, false);
            string encryptedBase64Password = Convert.ToBase64String(encodedPassword);


            SteamResult loginJson = null;
            CookieCollection cookieCollection;
            string steamGuardText = "";
            string steamGuardId = "";
            do
            {
                Console.WriteLine("SteamWeb: Logging In...");

                bool captcha = loginJson != null && loginJson.captcha_needed == true;
                bool steamGuard = loginJson != null && loginJson.emailauth_needed == true;

                string time = Uri.EscapeDataString(rsaJSON.timestamp);
                string capGID = loginJson == null ? null : Uri.EscapeDataString(loginJson.captcha_gid);

                data = new NameValueCollection();
                data.Add("password", encryptedBase64Password);
                data.Add("username", username);

                // Captcha
                string capText = "";
                if (captcha)
                {
                    Console.WriteLine("SteamWeb: Captcha is needed.");
                    System.Diagnostics.Process.Start("https://steamcommunity.com/public/captcha.php?gid=" + loginJson.captcha_gid);
                    Console.WriteLine("SteamWeb: Type the captcha:");
                    capText = Uri.EscapeDataString(Console.ReadLine());
                }

                data.Add("captchagid", captcha ? capGID : "");
                data.Add("captcha_text", captcha ? capText : "");
                // Captcha end

                // SteamGuard
                if (steamGuard)
                {
                    Console.WriteLine("SteamWeb: SteamGuard is needed.");
                    Console.WriteLine("SteamWeb: Type the code:");
                    steamGuardText = Uri.EscapeDataString(Console.ReadLine());
                    steamGuardId = loginJson.emailsteamid;
                }

                data.Add("emailauth", steamGuardText);
                data.Add("emailsteamid", steamGuardId);
                // SteamGuard end

                data.Add("rsatimestamp", time);

                using(HttpWebResponse webResponse = await Request("https://steamcommunity.com/login/dologin/", "POST", data, false))
                {
                    using(StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        string json = await reader.ReadToEndAsync();
                        loginJson = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<SteamResult>(json));
                        cookieCollection = webResponse.Cookies;
                    }
                }
            } while (loginJson.captcha_needed || loginJson.emailauth_needed);


            if (loginJson.success)
            {
                _cookies = new CookieContainer();
                foreach (Cookie cookie in cookieCollection)
                {
                    _cookies.Add(cookie);
                }
                SubmitCookies(_cookies);
                return true;
            }
            else
            {
                Console.WriteLine("SteamWeb Error: " + loginJson.message);
                return false;
            }

        }

        ///<summary>
        /// Authenticate using SteamKit2 and ISteamUserAuth. 
        /// This does the same as SteamWeb.DoLogin(), but without contacting the Steam Website.
        /// </summary> 
        /// <remarks>Should this one doesnt work anymore, use <see cref="SteamWeb.DoLogin"/></remarks>
        public bool Authenticate(string myUniqueId, SteamClient client, string myLoginKey)
        {
            Token = TokenSecure = "";
            SessionId = Convert.ToBase64String(Encoding.UTF8.GetBytes(myUniqueId));
            _cookies = new CookieContainer();

            using (dynamic userAuth = WebAPI.GetInterface("ISteamUserAuth"))
            {
                // generate an AES session key
                var sessionKey = CryptoHelper.GenerateRandomBlock(32);

                // rsa encrypt it with the public key for the universe we're on
                byte[] cryptedSessionKey = null;
                using (RSACrypto rsa = new RSACrypto(KeyDictionary.GetPublicKey(client.ConnectedUniverse)))
                {
                    cryptedSessionKey = rsa.Encrypt(sessionKey);
                }

                byte[] loginKey = new byte[20];
                Array.Copy(Encoding.ASCII.GetBytes(myLoginKey), loginKey, myLoginKey.Length);

                // aes encrypt the loginkey with our session key
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

                KeyValue authResult;

                try
                {
                    authResult = userAuth.AuthenticateUser(
                        steamid: client.SteamID.ConvertToUInt64(),
                        sessionkey: HttpUtility.UrlEncode(cryptedSessionKey),
                        encrypted_loginkey: HttpUtility.UrlEncode(cryptedLoginKey),
                        method: "POST",
                        secure: true
                        );
                }
                catch (Exception)
                {
                    Token = TokenSecure = null;
                    return false;
                }

                Token = authResult["token"].AsString();
                TokenSecure = authResult["tokensecure"].AsString();

                _cookies.Add(new Cookie("sessionid", SessionId, String.Empty, SteamCommunityDomain));
                _cookies.Add(new Cookie("steamLogin", Token, String.Empty, SteamCommunityDomain));
                _cookies.Add(new Cookie("steamLoginSecure", TokenSecure, String.Empty, SteamCommunityDomain));

                return true;
            }
        }

        /// <summary>
        /// Helper method to verify our precious cookies.
        /// </summary>
        /// <param name="cookies">CookieContainer with our cookies.</param>
        /// <returns>true if cookies are correct; false otherwise</returns>
        public async Task<bool> VerifyCookies()
        {
            using (HttpWebResponse response = await Request("http://steamcommunity.com/", "HEAD"))
            {
                return response.Cookies["steamLogin"] == null || !response.Cookies["steamLogin"].Value.Equals("deleted");
            }
        }

        static async void SubmitCookies (CookieContainer cookies)
        {
            HttpWebRequest w = WebRequest.CreateHttp("https://steamcommunity.com/");

            w.Method = "POST";
            w.ContentType = "application/x-www-form-urlencoded";
            w.CookieContainer = cookies;

            (await w.GetResponseAsync()).Close();
        }

        private byte[] HexToByte(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];
            int l = hex.Length;

            for (int i = 0; i < (l >> 1); ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : 55);
        }

        public bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // allow all certificates
            return true;
        }

    }

    // JSON Classes
    public class GetRsaKey : SteamWeb.ResponseBase
    {
        public string publickey_mod { get; set; }

        public string publickey_exp { get; set; }

        public string timestamp { get; set; }
    }

    public class SteamResult : SteamWeb.ResponseBase
    {
        public string message { get; set; }

        public bool captcha_needed { get; set; }

        public string captcha_gid { get; set; }

        public bool emailauth_needed { get; set; }

        public string emailsteamid { get; set; }

    }

}
