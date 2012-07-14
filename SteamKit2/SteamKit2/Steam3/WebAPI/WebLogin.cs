using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using JsonFx.Json;

namespace SteamKit2
{
    /// <summary>
    /// Performs login using the API used by the Steam website.
    /// </summary>
    public static class WebLogin
    {
        const string API_RSA_KEY = "https://store.steampowered.com/login/getrsakey/";
        const string API_DO_LOGIN = "https://store.steampowered.com/login/dologin/";
        const string API_CRYPT = "http://127.0.0.1:1337/";

        public static string DoLogin(string username, string password)
        {
            var loginKey = GetKeyRSA(username);

            if (loginKey.Count < 4)
                throw new WebLoginException("Could not get expected RSA keys for login");

            var encryptedPassword = EncryptPassword(password, loginKey);
            var result = SendPassword(username, encryptedPassword, loginKey);

            return result;
        }

        private static string SendPassword(string username, string encryptedBase64, Dictionary<string, object> loginParams)
        {
            // Send the RSA-encrypted password via POST.
            var webRequest = WebRequest.Create(API_DO_LOGIN) as HttpWebRequest;
            webRequest.Method = "POST";

            var timestamp = loginParams["timestamp"] as string;

            var fields = new NameValueCollection();
            fields.Add("username", username);
            fields.Add("password", encryptedBase64);
            fields.Add("emailauth", String.Empty);
            fields.Add("captchagid", String.Empty);
            fields.Add("captcha_text", String.Empty);
            fields.Add("emailsteamid", String.Empty);
            fields.Add("rsatimestamp", timestamp);

            var query = fields.ConstructQueryString();
            var queryData = Encoding.ASCII.GetBytes(query);

            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = queryData.Length;
            webRequest.CookieContainer = new CookieContainer();

            // Write the request
            using (Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(queryData, 0, queryData.Length);
            }

            // Perform the request
            var response = webRequest.GetResponse() as HttpWebResponse;

            String res;
            using (Stream stream = response.GetResponseStream())
            {
                res = stream.ReadAll();
            }

            response.Close();

            var reader = new JsonReader();
            var results = reader.Read<Dictionary<string, object>>(res);

            return response.Cookies["steamLogin"].Value;
        }

        private static string EncryptPassword(string password, Dictionary<string, object> RSA)
        {
            // We prepend a "0" because else Parse will interpret the number as negative.
            string smod = "0" + RSA["publickey_mod"] as string;
            BigInteger mod = BigInteger.Parse(smod);//, NumberStyles.HexNumber);

            string sexp = "0" + RSA["publickey_exp"] as string;
            BigInteger exp = BigInteger.Parse(sexp);//, NumberStyles.HexNumber);

            var param = new RSAParameters();
            param.Modulus = mod.ToBigEndianByteArray();
            param.Exponent = exp.ToBigEndianByteArray();

            var crypto = new RSACryptoServiceProvider();
            crypto.ImportParameters(param);

            var encryptedData = crypto.Encrypt(Encoding.ASCII.GetBytes(password), false);
            var encryptedBase64 = Convert.ToBase64String(encryptedData);

            return encryptedBase64;
        }

        private static Dictionary<string, object> GetKeyRSA(string username)
        {
            // First ask the public RSA key for encryption.
            var webRequest = WebRequest.Create(API_RSA_KEY) as HttpWebRequest;
            webRequest.Method = "POST";

            var fields = new NameValueCollection();
            fields.Add("username", username);

            var query = fields.ConstructQueryString();
            byte[] data = Encoding.ASCII.GetBytes(query);

            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = data.Length;

            // Write the request
            using (Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            // Perform the request
            var response = webRequest.GetResponse();

            String res;
            using (Stream stream = response.GetResponseStream())
            {
                res = stream.ReadAll();
            }

            response.Close();

            var reader = new JsonReader();
            var json = reader.Read<Dictionary<string, object>>(res);

            return json;
        }
    }

    public class WebLoginException : Exception
    {
        public WebLoginException(string message)
            : base(message)
        {
        }
    }

    public static class WebLoginUtils
    {
        public static string ConstructQueryString(this NameValueCollection parameters)
        {
            var items = new List<string>();
            foreach (string name in parameters)
                items.Add(String.Concat(name, "=", Uri.EscapeDataString(parameters[name])));
            return string.Join("&", items.ToArray());
        }

        public static byte[] ToBigEndianByteArray(this BigInteger mod)
        {
            byte[] data = mod.ToByteArray();

            // Reverse the array to convert from little to big-endian.
            Array.Reverse(data);

            return data.Skip(1).ToArray();
        }

        public static string ReadAll(this Stream stream)
        {
            var sb = new StringBuilder();
            byte[] buf = new byte[8192];
            int count = 0;

            do
            {
                count = stream.Read(buf, 0, buf.Length);

                if (count != 0)
                {
                    var temp = Encoding.ASCII.GetString(buf, 0, count);
                    sb.Append(temp);
                }
            }
            while (count > 0);

            return sb.ToString();
        }
    }
}