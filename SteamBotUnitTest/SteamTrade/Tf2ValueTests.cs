using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SteamTrade;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    class TF2ValueTests
    {
        [Test]
        public void GetItemWorthStringKeysTest()
        {
            TestKeyOutputWithValue(TF2Value.Refined * 3);
            TestKeyOutputWithValue(TF2Value.Refined * 8);
            TestKeyOutputWithValue(TF2Value.Refined * 100);
            TestKeyOutputWithValue(TF2Value.Refined * 13 + TF2Value.Scrap * 8);
        }

        private void TestKeyOutputWithValue(TF2Value keyValue)
        {
            Assert.AreEqual("1 key", keyValue.ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys", (keyValue * 2).ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys + 0.11 ref", (keyValue * 2 + TF2Value.Scrap).ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys + 1 ref", (keyValue * 2 + TF2Value.Refined).ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys + 1.11 ref", (keyValue * 2 + TF2Value.Refined + TF2Value.Scrap).ToItemString(keyValue, "key"));
            Assert.AreEqual("0 keys", TF2Value.Zero.ToItemString(keyValue, "key"));
            Assert.AreEqual("1 ref", TF2Value.Refined.ToItemString(keyValue, "key"));
        }

        [Test]
        public void GetItemWorthStringFewScrapTest()
        {
            TF2Value cardWorth = TF2Value.Scrap * 4;
            Assert.AreEqual("1 card", cardWorth.ToItemString(cardWorth, "card"));
            Assert.AreEqual("2 cards", (cardWorth * 2).ToItemString(cardWorth, "card"));
            Assert.AreEqual("2 cards + 0.11 ref", (cardWorth * 2 + TF2Value.Scrap).ToItemString(cardWorth, "card"));
            Assert.AreEqual("0 cards", TF2Value.Zero.ToItemString(cardWorth, "card"));
            Assert.AreEqual("0.11 ref", TF2Value.Scrap.ToItemString(cardWorth, "card"));
        }

        [Test]
        public void GetItemWorthStringOneScrapTest()
        {
            TF2Value emoteWorth = TF2Value.Scrap;
            Assert.AreEqual("1 emote", emoteWorth.ToItemString(emoteWorth, "emote"));
            Assert.AreEqual("2 emotes", (emoteWorth * 2).ToItemString(emoteWorth, "emote"));
            Assert.AreEqual("3 emotes", (emoteWorth * 2 + TF2Value.Scrap).ToItemString(emoteWorth, "emote"));
            Assert.AreEqual("0 emotes", TF2Value.Zero.ToItemString(emoteWorth, "emote"));
        }

        [Test]
        public void GetItemWorthStringHalfScrapTest()
        {
            TF2Value weaponValue = TF2Value.Scrap / 2;
            Assert.AreEqual("1 weapon", weaponValue.ToItemString(weaponValue, "weapon"));
            Assert.AreEqual("2 weapons", (weaponValue * 2).ToItemString(weaponValue, "weapon"));
            Assert.AreEqual("4 weapons", (weaponValue * 2 + TF2Value.Scrap).ToItemString(weaponValue, "weapon"));
            Assert.AreEqual("0 weapons", TF2Value.Zero.ToItemString(weaponValue, "weapon"));
        }

        [Test]
        public void GetItemWorthStringPluralTest()
        {
            Assert.AreEqual("2 fish", (TF2Value.Refined * 2).ToItemString(TF2Value.Refined, "fish", "fish"));
            Assert.AreEqual("2 kitties", (TF2Value.Refined * 2).ToItemString(TF2Value.Refined, "kitty", "kitties"));
            Assert.AreEqual("0 kitties", TF2Value.Zero.ToItemString(TF2Value.Refined, "kitty", "kitties"));
        }

        [Test]
        public void GetItemWorthStringRoundDownTest()
        {
            Assert.AreEqual("1 dog", (TF2Value.Refined + TF2Value.Scrap).ToItemString(TF2Value.Refined, "dog", null, true));
            Assert.AreEqual("0 dogs", (TF2Value.Refined - TF2Value.Scrap).ToItemString(TF2Value.Refined, "dog", null, true));
        }

        [Test]
        public void MetalPartGetterTests()
        {
            TestGetters(1, 0, 0, 0);
            TestGetters(0, 1, 0, 0);
            TestGetters(0, 0, 1, 0);
            TestGetters(0, 0, 0, 1);
            TestGetters(1, 1, 1, 1);
            TestGetters(2, 2, 2, 2);
            TestGetters(0, 2, 2, 2);
            TestGetters(2, 0, 2, 2);
            TestGetters(2, 2, 0, 2);
            TestGetters(2, 2, 2, 0);
        }

        private void TestGetters(int refined, int reclaimed, int scrap, int grains)
        {
            TF2Value value = refined * TF2Value.Refined + reclaimed * TF2Value.Reclaimed + scrap * TF2Value.Scrap + grains * TF2Value.Grain;
            Assert.AreEqual(refined, value.RefinedPart);
            Assert.AreEqual(reclaimed, value.ReclaimedPart);
            Assert.AreEqual(scrap, value.ScrapPart);
            Assert.AreEqual(grains, value.GrainPart);
        }

        [Test]
        public void MetalTotalGetterTest()
        {
            TF2Value value = TF2Value.Refined + TF2Value.Reclaimed * 2; //1.66 ref
            Assert.AreEqual(5.0 / 3, value.RefinedTotal, 0.01);
            Assert.AreEqual(5, value.ReclaimedTotal);
            Assert.AreEqual(15, value.ScrapTotal);
        }

        [Test]
        public void MetalTotalGetterTest2()
        {
            TF2Value value = TF2Value.Scrap; //0.11 ref
            Assert.AreEqual(1.0 / 9, value.RefinedTotal, 0.01);
            Assert.AreEqual(1.0 / 3, value.ReclaimedTotal, 0.01);
            Assert.AreEqual(1, value.ScrapTotal);
        }

        [Test]
        public void GrainTotalGetterTest2()
        {
            Assert.AreEqual(TF2Value.Refined.GrainTotal, TF2Value.Scrap.GrainTotal * 9);
            Assert.AreEqual(TF2Value.Refined.GrainTotal, TF2Value.Reclaimed.GrainTotal * 3);
        }

        [Test]
        public void GetItemPartTest()
        {
            TF2Value keyPrice = 10 * TF2Value.Refined;
            TF2Value remainder;
            Assert.AreEqual(2, (25 * TF2Value.Refined).GetPriceUsingItem(keyPrice, out remainder));
            Assert.AreEqual(5 * TF2Value.Refined, remainder);
        }

        [Test]
        public void GetItemPartNoKeysTest()
        {
            TF2Value keyPrice = 10 * TF2Value.Refined;
            TF2Value remainder;
            Assert.AreEqual(0, (9 * TF2Value.Refined).GetPriceUsingItem(keyPrice, out remainder));
            Assert.AreEqual(9 * TF2Value.Refined, remainder);
        }

        [Test]
        public void GetItemPartNoRemainderTest()
        {
            TF2Value keyPrice = 10 * TF2Value.Refined;
            TF2Value remainder;
            Assert.AreEqual(2, (20 * TF2Value.Refined).GetPriceUsingItem(keyPrice, out remainder));
            Assert.AreEqual(TF2Value.Zero, remainder);
        }

        [Test]
        public void GetItemPartWorksWhenPassingSelfTest()
        {
            TF2Value keyPrice = 10 * TF2Value.Refined;
            TF2Value value = 25 * TF2Value.Refined;
            Assert.AreEqual(2, value.GetPriceUsingItem(keyPrice, out value));
            Assert.AreEqual(5 * TF2Value.Refined, value);
        }

        [Test]
        public void GetItemTotalTest()
        {
            TF2Value keyPrice = 10 * TF2Value.Refined;
            Assert.AreEqual(2.5, (25 * TF2Value.Refined).GetPriceUsingItem(keyPrice));
        }

        [Test]
        public void GetItemTotalNoKeysTest()
        {
            TF2Value keyPrice = 10 * TF2Value.Refined;
            Assert.AreEqual(0.5, (5 * TF2Value.Refined).GetPriceUsingItem(keyPrice));
        }

        [Test]
        public void GetItemTotalNoDecimalTest()
        {
            TF2Value keyPrice = 10 * TF2Value.Refined;
            Assert.AreEqual(2, (20 * TF2Value.Refined).GetPriceUsingItem(keyPrice));
        }

        [Test]
        public void GetRefTotalStringTest()
        {
            Assert.AreEqual("0.11 ref", (TF2Value.Scrap * 1).ToString());
            Assert.AreEqual("0.22 ref", (TF2Value.Scrap * 2).ToString());
            Assert.AreEqual("0.33 ref", (TF2Value.Scrap * 3).ToString());
            Assert.AreEqual("0.44 ref", (TF2Value.Scrap * 4).ToString());
            Assert.AreEqual("0.55 ref", (TF2Value.Scrap * 5).ToString());
            Assert.AreEqual("0.66 ref", (TF2Value.Scrap * 6).ToString());
            Assert.AreEqual("0.77 ref", (TF2Value.Scrap * 7).ToString());
            Assert.AreEqual("0.88 ref", (TF2Value.Scrap * 8).ToString());
            Assert.AreEqual("1 ref", (TF2Value.Scrap * 9).ToString());
            Assert.AreEqual("1.11 ref", (TF2Value.Scrap * 10).ToString());
            Assert.AreEqual("1.22 ref", (TF2Value.Scrap * 11).ToString());
            Assert.AreEqual("1.33 ref", (TF2Value.Scrap * 12).ToString());
            Assert.AreEqual("1.44 ref", (TF2Value.Scrap * 13).ToString());
            Assert.AreEqual("1.55 ref", (TF2Value.Scrap * 14).ToString());
            Assert.AreEqual("1.66 ref", (TF2Value.Scrap * 15).ToString());
            Assert.AreEqual("1.77 ref", (TF2Value.Scrap * 16).ToString());
            Assert.AreEqual("1.88 ref", (TF2Value.Scrap * 17).ToString());
            Assert.AreEqual("2 ref", (TF2Value.Scrap * 18).ToString());

            Assert.AreEqual("100 ref", (TF2Value.Refined * 100).ToString());
        }

        [Test]
        public void GetRefPartsStringTest()
        {
            Assert.AreEqual("1 scrap", (TF2Value.Scrap * 1).ToPartsString());
            Assert.AreEqual("2 scrap", (TF2Value.Scrap * 2).ToPartsString());
            Assert.AreEqual("1 rec", (TF2Value.Scrap * 3).ToPartsString());
            Assert.AreEqual("1 rec + 1 scrap", (TF2Value.Scrap * 4).ToPartsString());
            Assert.AreEqual("1 rec + 2 scrap", (TF2Value.Scrap * 5).ToPartsString());
            Assert.AreEqual("2 rec", (TF2Value.Scrap * 6).ToPartsString());
            Assert.AreEqual("2 rec + 1 scrap", (TF2Value.Scrap * 7).ToPartsString());
            Assert.AreEqual("2 rec + 2 scrap", (TF2Value.Scrap * 8).ToPartsString());
            Assert.AreEqual("1 ref", (TF2Value.Scrap * 9).ToPartsString());
            Assert.AreEqual("1 ref + 1 scrap", (TF2Value.Scrap * 10).ToPartsString());
            Assert.AreEqual("1 ref + 2 scrap", (TF2Value.Scrap * 11).ToPartsString());
            Assert.AreEqual("1 ref + 1 rec", (TF2Value.Scrap * 12).ToPartsString());
            Assert.AreEqual("1 ref + 1 rec + 1 scrap", (TF2Value.Scrap * 13).ToPartsString());
            Assert.AreEqual("1 ref + 1 rec + 2 scrap", (TF2Value.Scrap * 14).ToPartsString());
            Assert.AreEqual("1 ref + 2 rec", (TF2Value.Scrap * 15).ToPartsString());
            Assert.AreEqual("1 ref + 2 rec + 1 scrap", (TF2Value.Scrap * 16).ToPartsString());
            Assert.AreEqual("1 ref + 2 rec + 2 scrap", (TF2Value.Scrap * 17).ToPartsString());
            Assert.AreEqual("2 ref", (TF2Value.Scrap * 18).ToPartsString());

            Assert.AreEqual("100 ref", (TF2Value.Refined * 100).ToPartsString());
        }

        [Test]
        public void FromRefTest()
        {
            Assert.AreEqual(1, TF2Value.FromRef(0.11).ScrapTotal);
            Assert.AreEqual(2, TF2Value.FromRef(0.22).ScrapTotal);
            Assert.AreEqual(3, TF2Value.FromRef(0.33).ScrapTotal);
            Assert.AreEqual(4, TF2Value.FromRef(0.44).ScrapTotal);
            Assert.AreEqual(5, TF2Value.FromRef(0.55).ScrapTotal);
            Assert.AreEqual(6, TF2Value.FromRef(0.66).ScrapTotal);
            Assert.AreEqual(7, TF2Value.FromRef(0.77).ScrapTotal);
            Assert.AreEqual(8, TF2Value.FromRef(0.88).ScrapTotal);
            Assert.AreEqual(9, TF2Value.FromRef(1).ScrapTotal);
            Assert.AreEqual(10, TF2Value.FromRef(1.11).ScrapTotal);
            Assert.AreEqual(11, TF2Value.FromRef(1.22).ScrapTotal);
            Assert.AreEqual(12, TF2Value.FromRef(1.33).ScrapTotal);
            Assert.AreEqual(20, TF2Value.FromRef(2.22).ScrapTotal);

            Assert.AreEqual(9000, TF2Value.FromRef(1000).ScrapTotal);
            Assert.AreEqual(9001, TF2Value.FromRef(1000.11).ScrapTotal);
        }

        [Test]
        public void FromRefStringTest()
        {
            Assert.AreEqual(1, TF2Value.FromRef("0.11").ScrapTotal);
            Assert.AreEqual(2, TF2Value.FromRef("0.22").ScrapTotal);
            Assert.AreEqual(3, TF2Value.FromRef("0.33").ScrapTotal);
            Assert.AreEqual(4, TF2Value.FromRef("0.44").ScrapTotal);
            Assert.AreEqual(5, TF2Value.FromRef("0.55").ScrapTotal);
            Assert.AreEqual(6, TF2Value.FromRef("0.66").ScrapTotal);
            Assert.AreEqual(7, TF2Value.FromRef("0.77").ScrapTotal);
            Assert.AreEqual(8, TF2Value.FromRef("0.88").ScrapTotal);
            Assert.AreEqual(9, TF2Value.FromRef("1").ScrapTotal);
            Assert.AreEqual(10, TF2Value.FromRef("1.11").ScrapTotal);
            Assert.AreEqual(11, TF2Value.FromRef("1.22").ScrapTotal);
            Assert.AreEqual(12, TF2Value.FromRef("1.33").ScrapTotal);
            Assert.AreEqual(20, TF2Value.FromRef("2.22").ScrapTotal);
        }

        [Test]
        public void FromRefStringThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => TF2Value.FromRef(null));
            Assert.Throws<FormatException>(() => TF2Value.FromRef("eleven"));
        }

        [Test]
        public void DifferenceTest()
        {
            Assert.AreEqual(TF2Value.Zero, TF2Value.Difference(TF2Value.Refined, TF2Value.Refined));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Difference(TF2Value.Scrap * 8, TF2Value.Scrap * 9));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Difference(TF2Value.Scrap * 9, TF2Value.Scrap * 8));
            Assert.AreEqual(TF2Value.Refined, TF2Value.Difference(TF2Value.Scrap, TF2Value.Scrap + TF2Value.Refined));
        }

        [Test]
        public void MaxTest()
        {
            Assert.AreEqual(TF2Value.Refined, TF2Value.Max(TF2Value.Refined, TF2Value.Refined));
            Assert.AreEqual(TF2Value.Refined, TF2Value.Max(TF2Value.Refined, TF2Value.Scrap));
            Assert.AreEqual(TF2Value.Refined, TF2Value.Max(TF2Value.Scrap, TF2Value.Refined));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Max(TF2Value.Scrap, TF2Value.Zero));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Max(TF2Value.Zero, TF2Value.Scrap));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Max(TF2Value.Scrap, TF2Value.Grain));
            Assert.AreEqual(TF2Value.Grain, TF2Value.Max(TF2Value.Zero, TF2Value.Grain));
        }

        [Test]
        public void MinTest()
        {
            Assert.AreEqual(TF2Value.Refined, TF2Value.Min(TF2Value.Refined, TF2Value.Refined));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Min(TF2Value.Refined, TF2Value.Scrap));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Min(TF2Value.Scrap, TF2Value.Refined));
            Assert.AreEqual(TF2Value.Zero, TF2Value.Min(TF2Value.Scrap, TF2Value.Zero));
            Assert.AreEqual(TF2Value.Zero, TF2Value.Min(TF2Value.Zero, TF2Value.Refined));
            Assert.AreEqual(TF2Value.Grain, TF2Value.Min(TF2Value.Scrap, TF2Value.Grain));
            Assert.AreEqual(TF2Value.Zero, TF2Value.Min(TF2Value.Zero, TF2Value.Grain));
        }

        [Test]
        public void CeilingTest()
        {
            Assert.AreEqual(TF2Value.Zero, TF2Value.Ceiling(TF2Value.Zero));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Ceiling(TF2Value.Grain));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Ceiling(TF2Value.Scrap));
            Assert.AreEqual(TF2Value.Reclaimed, TF2Value.Ceiling(TF2Value.Reclaimed));
            Assert.AreEqual(TF2Value.Refined, TF2Value.Ceiling(TF2Value.Refined));

            Assert.AreEqual(TF2Value.Scrap, TF2Value.Ceiling(TF2Value.Scrap / 2));
            Assert.AreEqual(TF2Value.Refined + TF2Value.Scrap, TF2Value.Ceiling(TF2Value.Refined + TF2Value.Grain));
            Assert.AreEqual(TF2Value.Refined + TF2Value.Scrap, TF2Value.Ceiling(TF2Value.Refined + TF2Value.Scrap));
        }

        [Test]
        public void FloorTest()
        {
            Assert.AreEqual(TF2Value.Zero, TF2Value.Floor(TF2Value.Zero));
            Assert.AreEqual(TF2Value.Zero, TF2Value.Floor(TF2Value.Grain));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Floor(TF2Value.Scrap));
            Assert.AreEqual(TF2Value.Reclaimed, TF2Value.Floor(TF2Value.Reclaimed));
            Assert.AreEqual(TF2Value.Refined, TF2Value.Floor(TF2Value.Refined));

            Assert.AreEqual(TF2Value.Zero, TF2Value.Floor(TF2Value.Scrap / 2));
            Assert.AreEqual(TF2Value.Refined, TF2Value.Floor(TF2Value.Refined + TF2Value.Grain));
            Assert.AreEqual(TF2Value.Refined + TF2Value.Scrap, TF2Value.Floor(TF2Value.Refined + TF2Value.Scrap));
        }

        [Test]
        public void RoundTest()
        {
            Assert.AreEqual(TF2Value.Zero, TF2Value.Round(TF2Value.Zero));
            Assert.AreEqual(TF2Value.Zero, TF2Value.Round(TF2Value.Grain));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Round(TF2Value.Scrap));
            Assert.AreEqual(TF2Value.Reclaimed, TF2Value.Round(TF2Value.Reclaimed));
            Assert.AreEqual(TF2Value.Refined, TF2Value.Round(TF2Value.Refined));

            Assert.AreEqual(TF2Value.Zero, TF2Value.Round(TF2Value.Scrap / 2 - TF2Value.Grain));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Round(TF2Value.Scrap / 2));
            Assert.AreEqual(TF2Value.Scrap, TF2Value.Round(TF2Value.Scrap / 2 + TF2Value.Grain));

            Assert.AreEqual(TF2Value.Refined, TF2Value.Round(TF2Value.Refined + TF2Value.Scrap / 2 - TF2Value.Grain));
            Assert.AreEqual(TF2Value.Refined + TF2Value.Scrap, TF2Value.Round(TF2Value.Refined + TF2Value.Scrap / 2));
            Assert.AreEqual(TF2Value.Refined + TF2Value.Scrap, TF2Value.Round(TF2Value.Refined + TF2Value.Scrap / 2 + TF2Value.Grain));
        }

        [Test]
        public void SubtrationTest()
        {
            Assert.AreEqual(TF2Value.Reclaimed * 2, TF2Value.Refined - TF2Value.Reclaimed);
            Assert.AreEqual(TF2Value.Zero, TF2Value.Refined - TF2Value.Refined);
        }

        [Test]
        public void NegativeValueThrowsTest()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                TF2Value value = TF2Value.Scrap - TF2Value.Reclaimed;
            });

            Assert.Throws<ArgumentException>(() =>
            {
                TF2Value value = TF2Value.Reclaimed - TF2Value.Refined;
            });
        }

        [Test]
        public void DivisionTest()
        {
            Assert.AreEqual(TF2Value.Scrap, (TF2Value.Scrap * 3) / 3);
            Assert.AreEqual(3, (TF2Value.Scrap * 3) / TF2Value.Scrap);
        }

        [Test]
        public void ModulusTest()
        {
            Assert.AreEqual(TF2Value.Scrap, (2 * TF2Value.Refined + TF2Value.Scrap) % TF2Value.Refined);
        }
    }
}
