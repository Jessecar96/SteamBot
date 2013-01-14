using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.IO;
using System.Net.Security;
using SteamKit2;


namespace SteamBot.Trading
{
    public class Web
    {
        public string Domain { get; set; }
        public string Scheme { get; set; }
        public bool ActAsAjax { get; set; }
        public CookieContainer Cookies { get; private set; }

        public Web(CookieContainer cookies = null)
        {
            this.ActAsAjax = false;
            if (cookies != null)
            {
                this.Cookies = cookies;
            }
            else
            {
                this.Cookies = new CookieContainer();
            }
        }

        public Web(Web web)
        {
            this.ActAsAjax = web.ActAsAjax;
            this.Cookies = web.Cookies;
        }

        public string Do(string uri, string method = "GET")
        {
            return Do(uri, method, null);
        }

        public string Do(string uri, string method, NameValueCollection data)
        {
            HttpWebResponse response = Request(uri, method, data);
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public string Do(HttpWebRequest request)
        {
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public HttpWebResponse Request(string uri, string method = "GET")
        {
            return Request(uri, method, null);
        }

        public HttpWebResponse Request(string uri, string method, NameValueCollection data)
        {
            return GetRequest(uri, method, data).GetResponse() as HttpWebResponse;
        }

        public HttpWebRequest GetRequest(string uri, string method, NameValueCollection data)
        {
            HttpWebRequest request = WebRequest.Create(CreateUri(uri)) as HttpWebRequest;

            request.Method = method;
            request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Host = Domain;
            request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
            request.Referer = Scheme + "://" + Domain + "/trade/1";

            if (ActAsAjax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }

            request.CookieContainer = Cookies;

            if (data != null)
            {
                string dataString = String.Join("&", Array.ConvertAll(data.AllKeys, key =>
                        String.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
                    )
                );

                byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
                request.ContentLength = dataBytes.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(dataBytes, 0, dataBytes.Length);
            }

            return request;
        }

        private Uri CreateUri(string path)
        {
            Uri uri = new Uri(Scheme + "://" + Domain + path);
            return uri;
        }
    }
}
