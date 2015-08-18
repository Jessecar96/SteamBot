# CSGO Win Big Steam Bot

### What is this?
This is a custom fork of [https://github.com/Jessecar96/SteamBot](Jessecar96/SteamBot), for handling deposit and payout trade offers for [http://csgowinbig.jordanturley.com/](CSGO Win Big). CSGO Win Big is a Counter-Strike: Global Offensive jackpot skin betting website, created by me, Jordan Turley.

**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 8 contributors have all added to the bot.  The bot is publicly available under the MIT License. Check out [LICENSE] for more details.

There are several things you must do in order to get SteamBot working:

1. Download the source.
2. Compile the source code.
3. Configure the bot (username, password, etc.).
 * Be sure to set "BotControlClass" to "SteamBot.DepositTradeOfferUserHandler".
4. This project requires that you have Json.NET from Newtonsoft, so you will have to add this, probably through NuGet.
5. Download the code for the website [https://github.com/ztizzlegaming/CSGOWinBig](here), and upload it to a server, and set it up, making sure that it is functional.
6. Change the url [here](https://github.com/ztizzlegaming/CSGOWinBig-SteamBot/blob/master/SteamBot/DepositTradeOfferUserHandler.cs#L73) to your own website's deposit php script.
7. Once you have the website set up, you should have a file in your web root with your default password. Make a text file named 'password.txt' on your desktop (or wherever you want) with only this password in it. Then, update the location of this file [here](https://github.com/ztizzlegaming/CSGOWinBig-SteamBot/blob/master/SteamBot/DepositTradeOfferUserHandler.cs#L26).

## Getting the Source

Retrieving the source code should be done by following the [installation guide] on the wiki. The install guide covers the instructions needed to obtain the source code as well as the instructions for compiling the code.

## Configuring the Bot

See the [configuration guide] on the wiki. This guide covers configuring a basic bot as well as creating a custom user handler.

## Bot Administration

While running the bots you may find it necessary to do some basic operations like shutting down and restarting a bot. The console will take some commands to allow you to do some this. See the [usage guide] for more information.

## More help?
If it's a bug, open an Issue; if you have a fix, read [CONTRIBUTING.md] and open a Pull Request.  If it is a question about how to use SteamBot with your own bots, visit our subreddit at [/r/SteamBot](http://www.reddit.com/r/SteamBot). Please use the issue tracker only for bugs reports and pull requests. The subreddit should be used for all other  discussions.

## Wanna Contribute?
Please read [CONTRIBUTING.md].


   [installation guide]: https://github.com/Jessecar96/SteamBot/wiki/Installation-Guide
   [CONTRIBUTING.md]: https://github.com/Jessecar96/SteamBot/blob/master/CONTRIBUTING.md
   [LICENSE]: https://github.com/Jessecar96/SteamBot/blob/master/LICENSE
   [configuration guide]: https://github.com/Jessecar96/SteamBot/wiki/Configuration-Guide
   [usage guide]: https://github.com/Jessecar96/SteamBot/wiki/Usage-Guide
