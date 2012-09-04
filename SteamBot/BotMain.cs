using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace SteamBot
{
    public class BotMain
    {
        public static BotFile theBots;
        static void Main(string[] args)
        {
            TextReader tr = new StreamReader("settings.json");
            string text = tr.ReadToEnd();
            tr.Close();

            theBots = JsonConvert.DeserializeObject<BotFile>(text);

            for (int i = 0; i < theBots.Bots.Length; i++)
            {
                Console.WriteLine("Launching bot " + i);
                int i1 = i;
                new Thread(() =>
                {
                    int index = i1;

                    int numCrashes = 0;
                    while (numCrashes < 1000)
                    {
                        try
                        {
                            theBots.Bots[index].Admins = theBots.Admins;
                            Bot b = new Bot(theBots.Bots[index]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            numCrashes++;
                        }
                    }
                }).Start();
                Thread.Sleep(5000);
            }
        }
    }
}
