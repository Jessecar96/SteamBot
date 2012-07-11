using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using System.Collections;

using System.Numerics;
 
namespace SteamBot
{
	public class SteamWeb
	{
		public CookieCollection DoLogin (string user, string pass)
		{
			
			//Get RSA Key
			WebRequest wr = WebRequest.Create ("https://steamcommunity.com/login/getrsakey");
			byte[] data = Encoding.ASCII.GetBytes ("username=" +user);
			
			wr.Method = "POST";
			wr.ContentType = "application/x-www-form-urlencoded";
			wr.ContentLength = data.Length;
			
			//write it
			Stream post = wr.GetRequestStream ();
			post.Write (data, 0, data.Length);
			
			//get it
			Stream stream = wr.GetResponse ().GetResponseStream ();
			var res = new StreamReader (stream).ReadToEnd ();
			//JSON Convert
			GetRsaKey rsaJSON = JsonConvert.DeserializeObject<GetRsaKey>(res);
			
			
			//Validate
			if (rsaJSON.success != true) {
				//Failed RSA
				return null;
			} else {
				
				//RSA Encryption
				RSACryptoServiceProvider rsa = new RSACryptoServiceProvider ();
				RSAParameters param = new RSAParameters ();
				
				param.Exponent = HexToByte (rsaJSON.publickey_exp);
				param.Modulus = HexToByte (rsaJSON.publickey_mod);
				
				rsa.ImportParameters (param);
				
				byte[] bytePass = Encoding.ASCII.GetBytes (pass);
				byte[] encodedPass = rsa.Encrypt (bytePass, false);
				string passFinal = Convert.ToBase64String (encodedPass);
				
				
				SteamResult resJSON=null;
				HttpWebResponse end;
				string fnl;
				string capText=null;
				string time = null;
				byte[] dat;
				
				do{
					Console.WriteLine("SteamWeb: Logging In...");
					
					time = Uri.EscapeDataString(rsaJSON.timestamp);
					string capGID = (resJSON==null) ? (null) : (Uri.EscapeDataString(resJSON.captcha_gid));
					
					if(resJSON!=null && resJSON.captcha_needed==true){
						Console.WriteLine ("SteamWeb: Captcha is needed.");
						System.Diagnostics.Process.Start ("https://steamcommunity.com/public/captcha.php?gid=" + resJSON.captcha_gid);
						Console.WriteLine ("SteamWeb: Type the captcha:");
						capText = Uri.EscapeDataString(Console.ReadLine());
						
						
						dat = Encoding.ASCII.GetBytes (
							String.Format ("password={0}&username={1}&emailauth=&captchagid={3}&captcha_text={4}&emailsteamid=&rsatimestamp={2}",
					              Uri.EscapeDataString (passFinal), Uri.EscapeDataString (user), time, capGID, capText
					              )
						);
						
					}else{
						
						dat = Encoding.ASCII.GetBytes (
							String.Format ("password={0}&username={1}&emailauth=&captchagid=&captcha_text=&emailsteamid=&rsatimestamp={2}",
					              Uri.EscapeDataString (passFinal), Uri.EscapeDataString (user), time
					              )
						);
						
					}
					
					
					
					//NOW DO THE REQUEST!
					var w = WebRequest.Create ("https://steamcommunity.com/login/dologin/") as HttpWebRequest;
					
					
					w.Method = "POST";
					w.ContentType = "application/x-www-form-urlencoded";
					w.ContentLength = dat.Length;
					w.CookieContainer = new CookieContainer ();
				
					//write it
					Stream poot = w.GetRequestStream ();
					poot.Write (dat, 0, dat.Length);
				
					//get it
					end = w.GetResponse () as HttpWebResponse;
					fnl = new StreamReader (end.GetResponseStream ()).ReadToEnd ();
					end.Close ();
					
					resJSON = JsonConvert.DeserializeObject<SteamResult> (
	                      fnl
	             	);
					//Console.WriteLine("SteamWeb: "+fnl);
				}while(resJSON.captcha_needed==true);
				
				
				if(resJSON.success==true){
					
					//Return the cookies.
					return end.Cookies;
				}else{
					Console.WriteLine ("SteamWeb Error: "+resJSON.message);
					return null;	
				}
			}
			
			
			
		}
		
		//converters
		protected static byte[] HexToByte (string hex)
		{
			if (hex.Length % 2 == 1)
				throw new Exception ("The binary key cannot have an odd number of digits");

			byte[] arr = new byte[hex.Length >> 1];

			for (int i = 0; i < hex.Length >> 1; ++i) {
				arr [i] = (byte)((GetHexVal (hex [i << 1]) << 4) + (GetHexVal (hex [(i << 1) + 1])));
			}

			return arr;
		}
        
		protected static int GetHexVal (char hex)
		{
			int val = (int)hex;
			return val - (val < 58 ? 48 : 55);
		}
		
	}
	
	//JOSN Classes
	public class GetRsaKey
	{
        
		public bool success { get; set; }
        
		public string publickey_mod { get; set; }
        
		public string publickey_exp { get; set; }
        
		public string timestamp { get; set; }
	}
	
	public class SteamResult
	{
		
		public bool success{ get; set; }

		public string message{ get; set; }

		public bool captcha_needed{ get; set; }

		public string captcha_gid{ get; set; }
		
	}
}