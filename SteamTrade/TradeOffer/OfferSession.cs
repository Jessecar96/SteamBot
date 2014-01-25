using Newtonsoft.Json;
using SteamKit2;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace SteamTrade.TradeOffer
{
    public class OfferSession
    {
        private static readonly CookieContainer Cookies = new CookieContainer();

        public string SessionId { get; private set; }

        public string SteamLogin { get; private set; }

        public OfferSession(string sessionId, string token)
        {
            Cookies.Add(new Cookie("sessionid", sessionId, String.Empty, "steamcommunity.com"));
            Cookies.Add(new Cookie("steamLogin", token, String.Empty, "steamcommunity.com"));
            SessionId = sessionId;
            SteamLogin = token;
        }

        public string Fetch(string url, string method, NameValueCollection data = null, bool ajax = false, string referer = "")
        {
            try
            {
                HttpWebResponse response = Request(url, method, data, ajax, referer);
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (WebException we)
            {
                Debug.WriteLine(we);
            }
            return null;
        }

        public static HttpWebResponse Request(string url, string method, NameValueCollection data = null, bool ajax = false, string referer = "")
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

            request.Method = method;

            request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Host = "steamcommunity.com";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.3; en-US; Valve Steam Client/1389129507; ) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.166 Safari/535.19";
            request.Referer = string.IsNullOrEmpty(referer) ? "http://steamcommunity.com/tradeoffer/1" : referer;
            request.Headers.Add("Origin", "http://steamcommunity.com");

            request.CookieContainer = Cookies;

            if (ajax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }

            // Request data
            if (data != null)
            {
                string dataString = String.Join("&", Array.ConvertAll(data.AllKeys, key =>
                    String.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
                )
                );

                byte[] dataBytes = Encoding.ASCII.GetBytes(dataString);
                request.ContentLength = dataBytes.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(dataBytes, 0, dataBytes.Length);
            }

            // Get the response
            return request.GetResponse() as HttpWebResponse;
        }

        public bool Accept(string tradeOfferId, out string tradeId)
        {
            tradeId = "";
            var data = new NameValueCollection();
            data.Add("sessionid", SessionId);
            data.Add("tradeofferid", tradeOfferId);

            string url = string.Format("http://steamcommunity.com/tradeoffer/{0}/accept", tradeOfferId);
            string referer = string.Format("http://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            string resp = Fetch(url, "POST", data, false, referer);

            if (resp != null)
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<TradeOfferAcceptResponse>(resp);
                    if (!String.IsNullOrEmpty(result.TradeId))
                    {
                        tradeId = result.TradeId;
                        return true;
                    }
                    //todo: log the error
                    Debug.WriteLine(result.TradeError);
                }
                catch (JsonException jsex)
                {
                    Debug.WriteLine(jsex);
                }
            }
            return false;
        }

        public bool Decline(string tradeOfferId)
        {
            var data = new NameValueCollection();
            data.Add("sessionid", SessionId);
            data.Add("tradeofferid", tradeOfferId);

            string url = string.Format("http://steamcommunity.com/tradeoffer/{0}/decline", tradeOfferId);
            string referer = string.Format("http://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);
            var resp = Fetch(url, "POST", data, false, referer);

            if (resp != null)
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (json.TradeOfferId != null && json.TradeOfferId == tradeOfferId)
                    {
                        return true;
                    }
                }
                catch (JsonException jsex)
                {
                    Debug.WriteLine(jsex);
                }
            }
            return false;
        }

        public bool Cancel(string tradeOfferId)
        {
            var data = new NameValueCollection();
            data.Add("sessionid", SessionId);

            string url = string.Format("https://steamcommunity.com/tradeoffer/{0}/cancel", tradeOfferId);

            var resp = Fetch(url, "POST", data, false);

            if (resp != null)
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (json.TradeOfferId != null && json.TradeOfferId == tradeOfferId)
                    {
                        return true;
                    }
                }
                catch (JsonException jsex)
                {
                    Debug.WriteLine(jsex);
                }
            }
            return false;
        }

        public bool CounterOffer(string message, SteamID otherSteamId, TradeOffer.TradeStatus status, out string newTradeOfferId, string tradeOfferId = "")
        {
            newTradeOfferId = "";
            string jsonStatus = JsonConvert.SerializeObject(status, Formatting.None,
                new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.None });

            var data = new NameValueCollection();
            data.Add("sessionid", SessionId);
            data.Add("partner", otherSteamId.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", jsonStatus);
            if (!String.IsNullOrEmpty(tradeOfferId))
            {
                data.Add("tradeofferid_countered", tradeOfferId);
            }
            else
            {
                data.Add("trade_offer_create_params", "{}");
            }

            string url = "https://steamcommunity.com/tradeoffer/new/send";

            string resp = Fetch(url, "POST", data, false, "http://steamcommunity.com/tradeoffer/new/?partner=" + otherSteamId.AccountID);
            if (resp != null)
            {
                try
                {
                    var offerResponse = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (!String.IsNullOrEmpty(offerResponse.TradeOfferId))
                    {
                        newTradeOfferId = offerResponse.TradeOfferId;
                        return true;
                    }
                    else
                    {
                        //todo: log possible error
                        Debug.WriteLine(offerResponse.TradeError);
                    }
                }
                catch (JsonException jsex)
                {
                    Debug.WriteLine(jsex);
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a new trade offer
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <param name="newTradeOfferId">The trade offer id that will be created if successful</param>
        /// <returns>True if successfully returns a newTradeOfferId, else false</returns>
        public bool SendTradeOffer(string message, SteamID otherSteamId, TradeOffer.TradeStatus status, out string newTradeOfferId)
        {
            return CounterOffer(message, otherSteamId, status, out newTradeOfferId);
        }
    }

    public class NewTradeOfferResponse
    {
        [JsonProperty("tradeofferid")]
        public string TradeOfferId { get; set; }

        [JsonProperty("strError")]
        public string TradeError { get; set; }
    }

    public class TradeOfferAcceptResponse
    {
        [JsonProperty("tradeid")]
        public string TradeId { get; set; }

        [JsonProperty("strError")]
        public string TradeError { get; set; }
    }
}