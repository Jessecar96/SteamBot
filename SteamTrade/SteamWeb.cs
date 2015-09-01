using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SteamKit2;

namespace SteamTrade
{
    public sealed class SteamWeb
    {
        public const string SteamCommunityDomain = "steamcommunity.com";

        private CookieContainer cookies = new CookieContainer();

        public string Token { get; private set; }
        public string SessionId { get; private set; }
        public string TokenSecure { get; private set; }

        public Task<WebResponse> RequestAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "http://" + SteamCommunityDomain + "/trade/1")
        {
            bool isGet = method.ToUpper() == "GET";
            string dString = data == null ? null : string.Join("&", Array.ConvertAll(data.AllKeys, key =>
                string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
            ));
            if (isGet && !string.IsNullOrWhiteSpace(dString))
                url += (url.Contains("?") ? "&" : "?") + dString;
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Accept = "application/json, text/javascript;q=0.9, */*;q=0.5";
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
            if (!isGet && !string.IsNullOrEmpty(dString))
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(dString);
                request.ContentLength = dataBytes.Length;
                using (Stream requestStream = request.GetRequestStream())
                    requestStream.Write(dataBytes, 0, dataBytes.Length);
            }
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.CookieContainer = cookies;
            if (ajax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }
            request.Method = method.ToUpper();
            request.Referer = referer;
            request.Timeout = 50000;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
            return request.GetResponseAsync();
        }

        public HttpWebResponse Request(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "http://" + SteamCommunityDomain + "/trade/1")
        {
            Task<WebResponse> responseTask = RequestAsync(url, method, data, ajax, referer);
            responseTask.Wait();
            return responseTask.Result as HttpWebResponse;
        }

        public Task<string> FetchAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "http://" + SteamCommunityDomain + "/trade/1")
        {
            return RequestAsync(url, method, data, ajax, referer).ContinueWith(x => {
                using (WebResponse response = x.Result)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        return reader.ReadToEnd();
                }
            });
        }

        public string Fetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "http://" + SteamCommunityDomain + "/trade/1")
        {
            Task<string> responseTask = FetchAsync(url, method, data, ajax, referer);
            responseTask.Wait();
            return responseTask.Result;
        }

        ///<summary>
        /// Authenticate using SteamKit2 and ISteamUserAuth.
        /// </summary>
        /// <returns>True if authentication is successful, false otherwise.</returns>
        public Task<bool> AuthenticateAsync(string myUniqueId, SteamClient client, string myLoginKey)
        {
            Token = TokenSecure = "";
            SessionId = Convert.ToBase64String(Encoding.UTF8.GetBytes(myUniqueId));
            cookies = new CookieContainer();
            using (dynamic userAuth = WebAPI.GetAsyncInterface("ISteamUserAuth"))
            {
                var sessionKey = CryptoHelper.GenerateRandomBlock(32);
                byte[] cryptedSessionKey = null, loginKey = new byte[20];
                using (RSACrypto rsa = new RSACrypto(KeyDictionary.GetPublicKey(client.ConnectedUniverse)))
                    cryptedSessionKey = rsa.Encrypt(sessionKey);
                Array.Copy(Encoding.ASCII.GetBytes(myLoginKey), loginKey, myLoginKey.Length);
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);
                try
                {
                    Task<KeyValue> authResult = userAuth.AuthenticateUser(
                        steamid: client.SteamID.ConvertToUInt64(),
                        sessionkey: HttpUtility.UrlEncode(cryptedSessionKey),
                        encrypted_loginkey: HttpUtility.UrlEncode(cryptedLoginKey),
                        method: "POST",
                        secure: true
                        );
                    return authResult.ContinueWith(x =>
                    {
                        KeyValue result = x.Result;
                        Token = result["token"].AsString();
                        TokenSecure = result["tokensecure"].AsString();
                        cookies.Add(new Cookie("sessionid", SessionId, String.Empty, SteamCommunityDomain));
                        cookies.Add(new Cookie("steamLogin", Token, String.Empty, SteamCommunityDomain));
                        cookies.Add(new Cookie("steamLoginSecure", TokenSecure, String.Empty, SteamCommunityDomain));
                        return true;
                    });
                }
                catch (Exception)
                {
                    Token = TokenSecure = null;
                    return Task.Factory.StartNew(delegate () { return false; });
                }
            }
        }

        ///<summary>
        /// Authenticate using SteamKit2 and ISteamUserAuth.
        /// </summary>
        /// <returns>True if authentication is successful, false otherwise.</returns>
        public bool Authenticate(string myUniqueId, SteamClient client, string myLoginKey)
        {
            Task<bool> authTask = AuthenticateAsync(myUniqueId, client, myLoginKey);
            authTask.Wait();
            return authTask.Result;
        }

        /// <summary>
        /// Are the current cookies valid?
        /// </summary>
        /// <returns>True if cookies are valid, false otherwise</returns>
        public Task<bool> VerifyCookiesAsync()
        {
            return RequestAsync("http://" + SteamCommunityDomain, "HEAD").ContinueWith(x =>
            {
                using (HttpWebResponse response = x.Result as HttpWebResponse)
                    return response.Cookies["steamLogin"] == null || !response.Cookies["steamLogin"].Value.Equals("deleted");
            });
        }

        /// <summary>
        /// Are the current cookies valid?
        /// </summary>
        /// <returns>True if cookies are valid, false otherwise</returns>
        public bool VerifyCookies()
        {
            Task<bool> verifyTask = VerifyCookiesAsync();
            verifyTask.Wait();
            return verifyTask.Result;
        }

        public bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}