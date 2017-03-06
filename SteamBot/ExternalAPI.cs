using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteamBot
{
    class ExternalAPI
    {
        private Bot bot;
        private BackgroundWorker taskSearcher;
        private String JsonTask;
        private String apiUrl = "http://localhost/yii2/bets/webtask.php";
        private TaskResult taskResult;

        public ExternalAPI(Bot _bot)
        {
            bot = _bot;
            taskSearcher = new BackgroundWorker { WorkerSupportsCancellation = true };
            taskSearcher.DoWork += taskSearcher_DoWork;
            taskSearcher.RunWorkerAsync();
        }

        void taskSearcher_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!bot.IsLoggedIn)
                Thread.Sleep(2000);

            String url = apiUrl + "?steamid=" + bot.SteamClient.SteamID.ConvertToUInt64().ToString();


            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)WebRequest.Create(url).GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    JsonTask = reader.ReadToEnd();
                }
                //SchemaResult schemaResult = JsonConvert.DeserializeObject<SchemaResult>(result);
                
            }
            taskResult = JsonConvert.DeserializeObject<TaskResult>(JsonTask);
        }

        protected class TaskResult
        {
            public Schema result { get; set; }
        }
        protected class Schema
        {
            [JsonProperty("status")]
            public int Status { get; set; }

            [JsonProperty("tasks")]
            public WebTask[] Tasks { get; set; }
        }

        protected class WebTask
        {
            SteamKit2.SteamID steamID;

            [JsonProperty("steamid")]
            public ulong steamId
            {
                get
                {
                    return steamID.ConvertToUInt64();
                }
                set
                {
                    steamID = new SteamKit2.SteamID(value);
                }
            }
        }


    }
}
