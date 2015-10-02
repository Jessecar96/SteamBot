using System;
using System.IO;
using System.Collections.Specialized;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Web;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using SteamKit2;
// Resharper Comments to disabling naming of certain properties in classes.
// ReSharper disable InconsistentNaming

namespace SteamTrade
{
    /// <summary>
    /// SteamWeb class to create an API endpoint to the Steam Web.
    /// </summary>
    public class SteamWeb
    {
        /// <summary>
        /// Base steam community domain.
        /// </summary>
        public const string SteamCommunityDomain = "steamcommunity.com";

        /// <summary>
        /// Token of steam. Generated after login.
        /// </summary>
        public string Token { get; private set; }
        
        /// <summary>
        /// Session id of Steam after Login.
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Token secure as string. It is generated after the Login.
        /// </summary>
        public string TokenSecure { get; private set; }

        /// <summary>
        /// CookieContainer to save all cookies during the Login. 
        /// </summary>
        private CookieContainer _cookies = new CookieContainer();

        /// <summary>
        /// This method is using the Request method to return the full http stream from a web request as string.
        /// </summary>
        /// <param name="url">URL of the http request.</param>
        /// <param name="method">Gets the HTTP data transfer method (such as GET, POST, or HEAD) used by the client.</param>
        /// <param name="data">A NameValueCollection including Headers added to the request.</param>
        /// <param name="ajax">A bool to define if the http request is an ajax request.</param>
        /// <param name="referer">Gets information about the URL of the client's previous request that linked to the current URL.</param>
        /// <returns>The string of the http return stream.</returns>
        /// <remarks>If you want to know how the request method works, use: <see cref="SteamWeb.Request"/></remarks>
        public string Fetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "")
        {
            using (var response = Request(url, method, data, ajax, referer))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    // Catching the possibility if the Stream is null.
                    if (responseStream == null)
                    {
                        throw new NullReferenceException();
                    }
                    using (var reader = new StreamReader(responseStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// Custom wrapper for creating a HttpWebRequest, edited for Steam.
        /// </summary>
        /// <param name="url">Gets information about the URL of the current request.</param>
        /// <param name="method">Gets the HTTP data transfer method (such as GET, POST, or HEAD) used by the client.</param>
        /// <param name="data">A NameValueCollection including Headers added to the request.</param>
        /// <param name="ajax">A bool to define if the http request is an ajax request.</param>
        /// <param name="referer">Gets information about the URL of the client's previous request that linked to the current URL.</param>
        /// <returns>An instance of a HttpWebResponse object.</returns>
        public HttpWebResponse Request(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "")
        {
            // Append the data to the URL for GET-requests
            var isGetMethod = (method.ToLower() == "get");
            var dataString = (data == null ? null : string.Join("&", Array.ConvertAll(data.AllKeys, key =>
                $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(data[key])}"
                )));

            if (isGetMethod && !string.IsNullOrEmpty(dataString))
            {
                url += (url.Contains("?") ? "&" : "?") + dataString;
            }

            // Setup the request
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "application/json, text/javascript;q=0.9, */*;q=0.5";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            // request.Host is set automatically.
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
            request.Referer = string.IsNullOrEmpty(referer) ? "http://steamcommunity.com/trade/1" : referer;
            request.Timeout = 50000; // Timeout after 50 seconds.
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            if (ajax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }

            // Cookies
            request.CookieContainer = _cookies;

            // Write the data to the body for POST and other methods.
            if (isGetMethod || string.IsNullOrEmpty(dataString)) return request.GetResponse() as HttpWebResponse;

            // Just send the Stream if the Request is a POST
            var dataBytes = Encoding.UTF8.GetBytes(dataString);
            request.ContentLength = dataBytes.Length;

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(dataBytes, 0, dataBytes.Length);
            }

            // Get the response
            return request.GetResponse() as HttpWebResponse;
        }

        /// <summary>
        /// Executes the login by using the Steam Website.
        /// This Method is not used by Steambot repository, but it could be very helpful if you want to build a own Steambot or want to login into steam services like backpack.tf/csgolounge.com.
        /// Updated: 10-02-2015.
        /// </summary>
        /// <param name="username">Your Steam username.</param>
        /// <param name="password">Your Steam password.</param>
        /// <returns>A bool containing a value, if the login was successful.</returns>
        public bool DoLogin(string username, string password)
        {
            var data = new NameValueCollection {{"username", username}};
            var response = Fetch("https://steamcommunity.com/login/getrsakey", "POST", data, false);
            var rsaJson = JsonConvert.DeserializeObject<GetRsaKey>(response);
            
            // Validate
            if (!rsaJson.success)
            {
                return false;
            }

            //RSA Encryption
            var rsa = new RSACryptoServiceProvider();
            var rsaParameters = new RSAParameters
            {
                Exponent = HexToByte(rsaJson.publickey_exp),
                Modulus = HexToByte(rsaJson.publickey_mod)
            };

            rsa.ImportParameters(rsaParameters);

            var bytePassword = Encoding.ASCII.GetBytes(password);
            var encodedPassword = rsa.Encrypt(bytePassword, false);
            var encryptedBase64Password = Convert.ToBase64String(encodedPassword);
            
            SteamResult loginJson = null;
            CookieCollection cookieCollection;
            var steamGuardText = "";
            var steamGuardId = "";
            do
            {
                Console.WriteLine("SteamWeb: Logging In...");

                var captcha = loginJson != null && loginJson.captcha_needed;
                var steamGuard = loginJson != null && loginJson.emailauth_needed;

                var time = Uri.EscapeDataString(rsaJson.timestamp);
                var capGid = string.Empty;
                if (loginJson?.captcha_gid != null)
                {
                    capGid = Uri.EscapeDataString(loginJson.captcha_gid);
                }
                
                data = new NameValueCollection {{"password", encryptedBase64Password}, {"username", username}};

                // Captcha
                var capText = "";
                if (captcha)
                {
                    Console.WriteLine("SteamWeb: Captcha is needed.");
                    System.Diagnostics.Process.Start("https://steamcommunity.com/public/captcha.php?gid=" + loginJson.captcha_gid);
                    Console.WriteLine("SteamWeb: Type the captcha:");
                    var consoleText = Console.ReadLine();
                    capText = string.IsNullOrEmpty(consoleText) ? "" : Uri.EscapeDataString(consoleText);
                    
                }

                data.Add("captchagid", captcha ? capGid : "");
                data.Add("captcha_text", captcha ? capText : "");
                data.Add("twofactorcode", "");
                data.Add("remember_login", "false");
                // Captcha end

                // SteamGuard
                if (steamGuard)
                {
                    Console.WriteLine("SteamWeb: SteamGuard is needed.");
                    Console.WriteLine("SteamWeb: Type the code:");
                    var consoleText = Console.ReadLine();
                    steamGuardText = string.IsNullOrEmpty(consoleText) ? "" : Uri.EscapeDataString(consoleText);
                    steamGuardId = loginJson.emailsteamid;
                    Console.WriteLine("SteamWeb: Type your machine name:");
                    consoleText = Console.ReadLine();
                    var machineName = string.IsNullOrEmpty(consoleText) ? "" : Uri.EscapeDataString(consoleText);
                    data.Add("loginfriendlyname", machineName != "" ? machineName : "defaultSteamBotMachine");
                }

                data.Add("emailauth", steamGuardText);
                data.Add("emailsteamid", steamGuardId);
                var unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                data.Add("donotcache", unixTimestamp + "000");
                // SteamGuard end

                data.Add("rsatimestamp", time);

                using(var webResponse = Request("https://steamcommunity.com/login/dologin/", "POST", data, false))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    using(var reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        var json = reader.ReadToEnd();
                        loginJson = JsonConvert.DeserializeObject<SteamResult>(json);
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

        /// <summary>
        /// Authenticate using SteamKit2 and ISteamUserAuth. 
        /// This does the same as SteamWeb.DoLogin(), but without contacting the Steam Website.
        /// </summary>
        /// <remarks>Should this one doesnt work anymore, use <see cref="SteamWeb.DoLogin"/></remarks>
        /// <param name="myUniqueId">Id what you get to login.</param>
        /// <param name="client">An instance of a SteamClient.</param>
        /// <param name="myLoginKey">Login Key of your account.</param>
        /// <returns></returns>
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
                byte[] cryptedSessionKey;
                using (var rsa = new RSACrypto(KeyDictionary.GetPublicKey(client.ConnectedUniverse)))
                {
                    cryptedSessionKey = rsa.Encrypt(sessionKey);
                }

                var loginKey = new byte[20];
                Array.Copy(Encoding.ASCII.GetBytes(myLoginKey), loginKey, myLoginKey.Length);

                // aes encrypt the loginkey with our session key
                var cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

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

                _cookies.Add(new Cookie("sessionid", SessionId, string.Empty, SteamCommunityDomain));
                _cookies.Add(new Cookie("steamLogin", Token, string.Empty, SteamCommunityDomain));
                _cookies.Add(new Cookie("steamLoginSecure", TokenSecure, string.Empty, SteamCommunityDomain));

                return true;
            }
        }

        /// <summary>
        /// Helper method to verify our precious cookies.
        /// </summary>
        /// <returns>true if cookies are correct; false otherwise</returns>
        public bool VerifyCookies()
        {
            using (var response = Request("http://steamcommunity.com/", "HEAD"))
            {
                return response.Cookies["steamLogin"] == null || !response.Cookies["steamLogin"].Value.Equals("deleted");
            }
        }

        /// <summary>
        /// Method to submit cookies to Steam after Login.
        /// </summary>
        /// <param name="cookies">Cookiecontainer which contains cookies after the login to Steam.</param>
        static void SubmitCookies (CookieContainer cookies)
        {
            var w = WebRequest.Create("https://steamcommunity.com/") as HttpWebRequest;

            if (w == null) return;
            w.Method = "GET";
            w.ContentType = "application/x-www-form-urlencoded";
            w.CookieContainer = cookies;

            w.GetResponse().Close();
        }

        /// <summary>
        /// Method to convert a Hex to a byte.
        /// </summary>
        /// <param name="hex">Input parameter as string.</param>
        /// <returns>The byte value.</returns>
        private static byte[] HexToByte(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");
            
            var arr = new byte[hex.Length >> 1];
            var l = hex.Length;

            for (var i = 0; i < (l >> 1); ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        /// <summary>
        /// Get the Hex value as int out of an char.
        /// </summary>
        /// <param name="hex">Input parameter.</param>
        /// <returns>A Hex Value as int.</returns>
        private static int GetHexVal(char hex)
        {
            var val = (int)hex;
            return val - (val < 58 ? 48 : 55);
        }

        /// <summary>
        /// Method to allow all certificates.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="policyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>Always true to accept all certificates.</returns>
        public bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

    }
    // JSON Classes
    /// <summary>
    /// Class to Deserialize the json response strings of the getResKey request. See: <see cref="SteamWeb.DoLogin"/>
    /// </summary>
    public class GetRsaKey
    {
        public bool success { get; set; }

        public string publickey_mod { get; set; }

        public string publickey_exp { get; set; }

        public string timestamp { get; set; }
    }

    /// <summary>
    /// Class to Deserialize the json response strings after the login. See: <see cref="SteamWeb.DoLogin"/>
    /// </summary>
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