using NUnit.Framework;
using SteamKit2;
using SteamTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    class GenericInventoryTest
    {
        [Test]
        public void LoadTest()
        {
            var inventory = new GenericInventory(new SteamWeb());
            //76561198104350201 is my friend's Steam account with lots of Dota2 items which will test the start/more way of loading. It's inventory will remain public and contain more than 2000 items long term.
            inventory.Load(570, new long[] { 2 }, new SteamID(76561198104350201));
            Assert.True(inventory.Errors.Count == 0 && inventory.Items.Count > 0 && inventory.Descriptions.Count > 0, string.Join("\r\n", inventory.Errors));
            Assert.True(inventory.Items.All(i => inventory.GetDescription(i.Value.assetid) != null));
        }
    }
}
