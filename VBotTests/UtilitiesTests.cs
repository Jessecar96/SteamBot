using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot.Tests
{
    [TestClass()]
    public class UtilitiesTests
    {
        [TestMethod()]
        public void GivenANormalMessage_AndAnEmptyListOfMatches_ItCannotStartWithAnyOfThoseMatches()
        {
            Assert.IsFalse(new SteamBot.Utilities().DoesMessageStartWith("this is my message", new List<string>(new string[] { })));
        }

        [TestMethod()]
        public void GivenANormalMessage_AndAnListOfMatchesThatItDoesNotStartWith_ItCannotStartWithAnyOfThoseMatches()
        {
            Assert.IsFalse(new SteamBot.Utilities().DoesMessageStartWith("this is my message", new List<string>(new string[] { "a", "list", "of", "words" })));
        }

        [TestMethod()]
        public void GivenANormalMessage_AndASingleMessageThatItMatches_ItDoesStartWithThatMatch()
        {
            Assert.IsTrue(new SteamBot.Utilities().DoesMessageStartWith("this is my message", new List<string>(new string[] { "this" })));
        }

        [TestMethod()]
        public void GivenANormalMessage_AndSomeWordsOfWhichAtLeastOneMatches_ItDoesStartWithThatMatch()
        {
            Assert.IsTrue(new SteamBot.Utilities().DoesMessageStartWith("this is my message", new List<string>(new string[] { "some", "words", "this" })));
        }

        [TestMethod()]
        public void GivenAMessageThatStartsWithACommand_HoweverTheresNoSpace_ItShouldNotMatch()
        {
            Assert.IsFalse(new SteamBot.Utilities().DoesMessageStartWith("!addreply something", new List<string>(new string[] { "!add" })));
        }

        [TestMethod()]
        public void GivenANormaMapName_ItIsUnchangedWhenSanitized()
        {
            Assert.AreEqual(new SteamBot.Utilities().SanitizeMapName("ctf_mexico_b1"), "ctf_mexico_b1");
        }

        [TestMethod()]
        public void GivenATypicalWorkshopMap_ItIsCleanedUp()
        {
            Assert.AreEqual(new SteamBot.Utilities().SanitizeMapName("workshop/arena_discovery_b4.ugc"), "arena_discovery_b4");
        }

        [TestMethod()]
        public void GivenATypicalWorkshopMapButWithATruncatedName_ItIsCleanedUp()
        {
            Assert.AreEqual(new SteamBot.Utilities().SanitizeMapName("workshop/arena_discovery123_b4.u"), "arena_discovery123_b4");
        }
    }
}