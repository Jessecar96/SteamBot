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

namespace SteamBot
{
	public class SteamWeb
	{

		public static string Fetch (string url, string method, NameValueCollection data = null, CookieContainer cookies = null, bool ajax = true)
		{
			HttpWebResponse response = Request (url, method, data, cookies, ajax);
			StreamReader reader = new StreamReader (response.GetResponseStream ());
			return reader.ReadToEnd ();
		}

		public static HttpWebResponse Request (string url, string method, NameValueCollection data = null, CookieContainer cookies = null, bool ajax = true)
		{
			HttpWebRequest request = WebRequest.Create (url) as HttpWebRequest;
			
			request.Method = method;
			
			request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
			request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			request.Host = "steamcommunity.com";
			request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
			request.Referer = "http://steamcommunity.com/trade/1";
			
			if (ajax) {
				request.Headers.Add ("X-Requested-With", "XMLHttpRequest");
				request.Headers.Add ("X-Prototype-Version", "1.7");
			}
			
			// Cookies
			request.CookieContainer = cookies ?? new CookieContainer ();
			
			// Request data
			if (data != null) {
				string dataString = String.Join ("&", Array.ConvertAll (data.AllKeys, key =>
					String.Format ("{0}={1}", HttpUtility.UrlEncode (key), HttpUtility.UrlEncode (data [key]))
				)
				);
				
				byte[] dataBytes = Encoding.ASCII.GetBytes (dataString);
				request.ContentLength = dataBytes.Length;
				
				Stream requestStream = request.GetRequestStream ();
				requestStream.Write (dataBytes, 0, dataBytes.Length);
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
			if (rsaJSON.success != true) {
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
			do {
				Console.WriteLine ("SteamWeb: Logging In...");

				bool captcha = loginJson != null && loginJson.captcha_needed == true;

				string time = Uri.EscapeDataString (rsaJSON.timestamp);
				string capGID = loginJson == null ? null : Uri.EscapeDataString (loginJson.captcha_gid);

				data = new NameValueCollection ();
				data.Add ("password", encryptedBase64Password);
				data.Add ("username", username);
				data.Add ("emailauth", "");

				// Captcha
				string capText = "";
				if (captcha) {
					Console.WriteLine ("SteamWeb: Captcha is needed.");
					System.Diagnostics.Process.Start ("https://steamcommunity.com/public/captcha.php?gid=" + loginJson.captcha_gid);
					Console.WriteLine ("SteamWeb: Type the captcha:");
					capText = Uri.EscapeDataString (Console.ReadLine ());
				}

				data.Add ("captcha_gid", captcha ? capGID : "");
				data.Add ("captcha_text", captcha ? capText : "");
				// Captcha end

				data.Add ("emailsteamid", "");
				data.Add ("rsatimestamp", time);

				HttpWebResponse webResponse = Request ("https://steamcommunity.com/login/dologin/", "POST", data, null, false);

				StreamReader reader = new StreamReader (webResponse.GetResponseStream ());
				string json = reader.ReadToEnd ();

				loginJson = JsonConvert.DeserializeObject<SteamResult> (json);

				cookies = webResponse.Cookies;
			} while (loginJson.captcha_needed == true);


			if (loginJson.success == true) {
				CookieContainer c = new CookieContainer ();
				foreach (Cookie cookie in cookies) {
					c.Add (cookie);
				}
				SubmitCookies (c);
				return cookies;
			} else {
				Console.WriteLine ("SteamWeb Error: " + loginJson.message);
				return null;
			}
            
		}

		static void SubmitCookies (CookieContainer cookies)
		{
			var w = WebRequest.Create ("https://steamcommunity.com/") as HttpWebRequest;

			w.Method = "POST";
			w.ContentType = "application/x-www-form-urlencoded";
			w.CookieContainer = cookies;

			w.GetResponse ().Close ();
			// Why would you need to do this?  Reading the response isn't nessicary, since
			// you submitted the request already.
			//string result = new StreamReader (response.GetResponseStream ()).ReadToEnd ();
			//response.Close ();
			return;
		}
		
		static byte[] HexToByte (string hex)
		{
			if (hex.Length % 2 == 1)
				throw new Exception ("The binary key cannot have an odd number of digits");

			byte[] arr = new byte[hex.Length >> 1];
			int l = hex.Length;

			for (int i = 0; i < (l >> 1); ++i) {
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

	}

}