SteamBot is a bot for interacting with Steam Chat and Trading.

## Changelog ##

Tue Sep 4 12:00 PM
- Rewrote pretty much the entire trading system
- Added configuration via a settings.json file
- Removed multithreading (caused too many issues)
- Added Steam authentication (removes need for captchas)
- Added a base TradeSystem.cs class that can be extended for multiple types of trades
- Added an example trade type, TradeEnterRaffle.

Wed Jul 11 4:11 AM
- Steam Trading is working!
- Began adding Trading API
- Fixed trading request cookies
- Added basic trade event logging
- cleaned up more things


Tue Jul 10, 11:58 PM
- Updated readme
- removed chat commands to get redy for api
- Tried to fix steam trading, still not working
- cleaned up some things
