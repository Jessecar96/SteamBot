**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 8 contributors have all added to the bot.  The bot is publicly available under the MIT License. Check out [LICENSE] for more details.

**DO NOT DOWNLOAD THE ZIP FROM GITHUB!**

This bot requires you use git to download the bot. *Downloading the zip will not work!* It doesn't work because the bot uses git submodules, which are not included with the zip download.

There are several things you must do in order to get SteamBot working:

1. Download the source using Git.
2. Compile the source code.
3. Configure the bot (username, password, etc.).
4. *Optionally*, customize the bot by changing the source code.

## Getting the Source

**AGAIN: DO NOT DOWNLOAD THE ZIP FROM GITHUB!**

Retrieving the source code should be done by following the [installation guide] on the wiki. The install guide covers the instructions needed to obtain the source code as well as the instructions for compiling the code.

## Configuring the Bot

Next you need to actually edit the bot to make it do what you want. You can edit the files `SimpleUserHandler.cs` or `AdminUserHandler.cs` or you can create your very own `UserHandler`. See the [configuration guide] on the wiki. This guide covers configuring a basic bot as well as creating a custom user handler.

## More help?
If it's a bug, open an Issue; if you have a fix, read [CONTRIBUTING.md] and open a Pull Request.  A list of contributors (add yourself if you want to):

- [Jessecar96](http://steamcommunity.com/id/jessecar) (project lead)
- [geel9](http://steamcommunity.com/id/geel9)
- [Dr. Cat, MD or redjazz96](http://steamcommunity.com/id/redjazz96)
- [cwhelchel](http://steamcommunity.com/id/cmw69krinkle)

## Wanna Contribute?
Please read [CONTRIBUTING.md].


   [installation guide]: https://github.com/Jessecar96/SteamBot/wiki/Installation-Guide
   [CONTRIBUTING.md]: https://github.com/Jessecar96/SteamBot/blob/master/CONTRIBUTING.md
   [LICENSE]: https://github.com/Jessecar96/SteamBot/blob/master/LICENSE
   [configuration guide]: https://github.com/Jessecar96/SteamBot/wiki/Configuration-Guide
