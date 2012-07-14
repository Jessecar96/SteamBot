using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SteamKit2;

namespace SteamBot
{
    class Program
    {

        public static SteamClient steamClient;
        static string[] admins = { "STEAM_0:1:25578691" };
        static SteamFriends steamFriends;
        static List<SteamID> clients = new List<SteamID>();


        static void printConsole(String line,ConsoleColor color = ConsoleColor.White)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(line);
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        static public bool checkAdmin(SteamID sid)
        {
            if (admins.Contains(sid.ToString()))
                return true;
            else
                steamFriends.SendChatMessage(sid, EChatEntryType.ChatMsg, "You cannot use this command because you're not an admin.");
                return false;
        }


        static void Main(string[] args)
        {


            

            Console.ForegroundColor = ConsoleColor.DarkCyan;

            System.Console.Title = "TradeBot";
            System.Console.WriteLine("Welcome to TradeBot!\nCreated by Jessecar.\nTurn of Steam Guard before loggin in!\n\n");

            Console.ForegroundColor = ConsoleColor.White;

            printConsole("Steam Username:");
            String username = "jessecar96"; //Console.ReadLine();

            System.Console.WriteLine("Steam Password: ");

            //heckey
            Console.ForegroundColor = Console.BackgroundColor;

            String password = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.White;

            SteamClient steamClient = new SteamClient(); // initialize our client
            SteamUser steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            SteamTrading trade = steamClient.GetHandler<SteamTrading>();

            steamClient.Connect(); // connect to the steam network

            while (true)
            {

                if (Console.KeyAvailable)
                {
                    printConsole(Console.ReadLine(), ConsoleColor.Yellow);
                }

                CallbackMsg msg = steamClient.WaitForCallback(true); // block and wait until a callback is posted

                //Print out callbacks
                //printConsole(msg.ToString());


                //Steam Connection
                msg.Handle<SteamClient.ConnectedCallback>(callback =>
                {
                    if (callback.Result != EResult.OK)
                    {
                        printConsole("Sorry, could not connect to Steam.");
                    }
                    steamUser.LogOn(new SteamUser.LogOnDetails
                    {
                        Username = username,
                        Password = password,
                    });
                });


                //Login Callback
                msg.Handle<SteamUser.LoggedOnCallback>(callback =>
                {
                    if (callback.Result != EResult.OK)
                    {
                        printConsole("Incorrect username or Password. Make sure you have disabled steam guard!");
                    }
                    else
                    {
                        printConsole("Connected to Steam!\nWelcome "+steamUser.SteamID);
                        steamFriends.SetPersonaName("ChatBot Beta (Say hi)");
                        steamFriends.SetPersonaState((EPersonaState)6);
                    }
                });

                //Chat Messages
                msg.Handle<SteamFriends.FriendMsgCallback>(callback =>
                {
                    EChatEntryType type = callback.EntryType;

                    

                    if (type == EChatEntryType.ChatMsg)
                    {

                        SteamID sid = callback.Sender;

                        if (!clients.Contains(callback.Sender))
                        {
                            printConsole("[New Client]" + callback.Sender, ConsoleColor.Magenta);
                            clients.Add(callback.Sender);

                            steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Welcome to TradeBot created by Jessecar.  To see a list of commands type /help");
                        }

                        if (callback.Message.StartsWith("/"))
                        {
                            
                            string message = callback.Message.Replace("/", "");

                            printConsole("[Command]" + callback.Sender + " (" + steamFriends.GetFriendPersonaName(callback.Sender) + "): " + message, ConsoleColor.Magenta);
                            //string[] args = .Split(" ");

                            string[] words = message.Split(new char[] { ' ' }, 2);

                            switch (words[0])
                            {
                                case "trade":
                                    //Send a trade
                                    trade.RequestTrade(callback.Sender);
                                    printConsole("Trade requested by " + callback.Sender + " (" + steamFriends.GetFriendPersonaName(callback.Sender) + ")", ConsoleColor.Green);
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.Emote, "initiated a trade request.");
                                    break;
                                case "remove":
                                    //Remove Friend
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Thank you for using the Steam TradeBot BETA.");
                                    steamFriends.RemoveFriend(callback.Sender);
                                    printConsole("[Friend] Friend Removed: " + callback.Sender + " (" + steamFriends.GetFriendPersonaName(callback.Sender) + ")", ConsoleColor.Yellow);
                                    break;
                                case "status":
                                    //get status (nothing)
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.Emote, "is Online and working good.");
                                    break;
                                case "hi":
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.Emote, "says hello.");
                                    break;
                                case "help":
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "\nList Of Commands:\n/trade - Start a trade.\n/remove - Remove TradeBot from your friends.\n/hi - say hello");
                                    break;
                                case "name":
                                    if(checkAdmin(sid))
                                        steamFriends.SetPersonaName(words[1]);
                                    break;
                                case "send":
                                    string[] wrds = message.Split(new char[] { ' ' }, 3);

                                    int index = int.Parse(wrds[1]);

                                    if(index<clients.Count() && index>=0)
                                        steamFriends.SendChatMessage(clients[index], EChatEntryType.ChatMsg, wrds[2]);
                                    else
                                        steamFriends.SendChatMessage(callback.Sender, EChatEntryType.Emote, "Error: index out of bounds.");

                                    break;
                                default:
                                    printConsole("[Error]Unknown command from " + callback.Sender + ": " + callback.Message, ConsoleColor.Red);
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.Emote, "doesn't know that command.");
                                    break;
                            }
                        }
                        else
                        {
                            printConsole("[Chat][" + getIndex(sid) + "]" + callback.Sender + ": " + " (" + steamFriends.GetFriendPersonaName(callback.Sender) + ")" + callback.Message, ConsoleColor.Magenta);
                            if ((callback.Message != "hi" || callback.Message != "hello") && clients.Contains(callback.Sender))
                            {
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "You Said: " + callback.Message);
                            }

                        }
                    }
                    else if (type == EChatEntryType.Emote)
                    {
                        printConsole("[Emote]" + callback.Sender + ": " + callback.Message, ConsoleColor.DarkMagenta);
                    }
                });

                msg.Handle<SteamTrading.TradeProposedCallback>(callback =>
                {
                    SteamID sid = callback.Other;
                    //trade.RespondTradeRequest(callback.TradeRequestId, sid, true);
                    //trade.HandleMsg((IPacketMsg)EMsg.EconTrading_InitiateTradeProposed);

                });

                msg.Handle<SteamTrading.TradeRequestCallback>(callback =>
                {
                    printConsole("[Trade] Trade Status with " + callback.Other + " (" + steamFriends.GetFriendPersonaName(callback.Other) + "): " + callback.Status.ToString(), ConsoleColor.Green);
                    if (callback.Status == ETradeStatus.Rejected)
                    {
                        printConsole("[Trade] Trade rejected by " + callback.Other + " (" + steamFriends.GetFriendPersonaName(callback.Other) + ")", ConsoleColor.DarkRed);
                        steamFriends.SendChatMessage(callback.Other, EChatEntryType.Emote, "detected that you rejected that trade.");
                    }
                    //trade.RespondTradeRequest(callback.TradeRequestId, callback.Other, true);
                    
                });

                msg.Handle<SteamTrading.TradeStartSessionCallback>(callback =>
                {
                    //callback.Other
                });

                msg.Handle<SteamFriends.PersonaStateCallback>(callback =>
                {
                    if (callback.FriendID == steamUser.SteamID)
                        return;

                    EFriendRelationship relationship = steamFriends.GetFriendRelationship(callback.FriendID);
                    if (!(relationship == EFriendRelationship.RequestRecipient))
                        return;

                    printConsole("[Friend] Added Friend: " + callback.FriendID + "(" + steamFriends.GetFriendPersonaName(callback.FriendID) + ")", ConsoleColor.Yellow);
                    steamFriends.AddFriend(callback.FriendID);
                });

            }

        }

        public static int getIndex(SteamID sid)
        {
            for(int i=0;i<clients.Count();i++)
                if (clients[i] == sid)
                    return i;
            return -1;
        }

    }
}
;