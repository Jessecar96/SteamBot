using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SteamBot.Trading.Traders
{
    class ConsoleTrader : ITrader
    {

        public Trade trade { get; set; }

        public void OnStatusUpdate(Api.Status status)
        {
            trade.DoLog(ELogType.DEBUG, 
                String.Format("STATUS:\nerror: {0}, new_version: {1}, sucess: {2}, trade_status: {3}, version: {4}, logpos: {5}",
                status.Error, status.NewVersion, status.Success, status.TradeStatus, status.Version, status.Logpos));
            if (status.Success == false)
                trade.trading = false;
        }
    }
}
