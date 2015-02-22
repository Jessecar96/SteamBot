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
    class Tf2ValueTests
    {
        [Test]
        public void GetItemWorthStringKeysTest()
        {
            TestKeyOutputWithValue(Tf2Value.Refined * 3);
            TestKeyOutputWithValue(Tf2Value.Refined * 8);
            TestKeyOutputWithValue(Tf2Value.Refined * 100);
            TestKeyOutputWithValue(Tf2Value.Refined * 13 + Tf2Value.Scrap * 8);
        }

        private void TestKeyOutputWithValue(Tf2Value keyValue)
        {
            Assert.AreEqual("1 key", keyValue.ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys", (keyValue * 2).ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys + 0.11 ref", (keyValue * 2 + Tf2Value.Scrap).ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys + 1 ref", (keyValue * 2 + Tf2Value.Refined).ToItemString(keyValue, "key"));
            Assert.AreEqual("2 keys + 1.11 ref", (keyValue * 2 + Tf2Value.Refined + Tf2Value.Scrap).ToItemString(keyValue, "key"));
            Assert.AreEqual("0 keys", Tf2Value.Zero.ToItemString(keyValue, "key"));
            Assert.AreEqual("1 ref", Tf2Value.Refined.ToItemString(keyValue, "key"));
        }

        [Test]
        public void GetItemWorthStringFewScrapTest()
        {
            Tf2Value cardWorth = Tf2Value.Scrap * 4;
            Assert.AreEqual("1 card", cardWorth.ToItemString(cardWorth, "card"));
            Assert.AreEqual("2 cards", (cardWorth * 2).ToItemString(cardWorth, "card"));
            Assert.AreEqual("2 cards + 0.11 ref", (cardWorth * 2 + Tf2Value.Scrap).ToItemString(cardWorth, "card"));
            Assert.AreEqual("0 cards", Tf2Value.Zero.ToItemString(cardWorth, "card"));
            Assert.AreEqual("0.11 ref", Tf2Value.Scrap.ToItemString(cardWorth, "card"));
        }

        [Test]
        public void GetItemWorthStringOneScrapTest()
        {
            Tf2Value emoteWorth = Tf2Value.Scrap;
            Assert.AreEqual("1 emote", emoteWorth.ToItemString(emoteWorth, "emote"));
            Assert.AreEqual("2 emotes", (emoteWorth * 2).ToItemString(emoteWorth, "emote"));
            Assert.AreEqual("3 emotes", (emoteWorth * 2 + Tf2Value.Scrap).ToItemString(emoteWorth, "emote"));
            Assert.AreEqual("0 emotes", Tf2Value.Zero.ToItemString(emoteWorth, "emote"));
        }

        [Test]
        public void GetItemWorthStringHalfScrapTest()
        {
            Tf2Value weaponValue = Tf2Value.Scrap / 2;
            Assert.AreEqual("1 weapon", weaponValue.ToItemString(weaponValue, "weapon"));
            Assert.AreEqual("2 weapons", (weaponValue * 2).ToItemString(weaponValue, "weapon"));
            Assert.AreEqual("4 weapons", (weaponValue * 2 + Tf2Value.Scrap).ToItemString(weaponValue, "weapon"));
            Assert.AreEqual("0 weapons", Tf2Value.Zero.ToItemString(weaponValue, "weapon"));
        }

        [Test]
        public void GetItemWorthStringPluralTest()
        {
            Assert.AreEqual("2 fish", (Tf2Value.Refined * 2).ToItemString(Tf2Value.Refined, "fish", "fish"));
            Assert.AreEqual("2 kitties", (Tf2Value.Refined * 2).ToItemString(Tf2Value.Refined, "kitty", "kitties"));
            Assert.AreEqual("0 kitties", Tf2Value.Zero.ToItemString(Tf2Value.Refined, "kitty", "kitties"));
        }

        [Test]
        public void GetItemWorthStringRoundDownTest()
        {
            Assert.AreEqual("1 dog", (Tf2Value.Refined + Tf2Value.Scrap).ToItemString(Tf2Value.Refined, "dog", null, true));
            Assert.AreEqual("0 dogs", (Tf2Value.Refined - Tf2Value.Scrap).ToItemString(Tf2Value.Refined, "dog", null, true));
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
            Tf2Value value = refined * Tf2Value.Refined + reclaimed * Tf2Value.Reclaimed + scrap * Tf2Value.Scrap + grains * Tf2Value.Grain;
            Assert.AreEqual(refined, value.RefinedPart);
            Assert.AreEqual(reclaimed, value.ReclaimedPart);
            Assert.AreEqual(scrap, value.ScrapPart);
            Assert.AreEqual(grains, value.GrainPart);
        }

        [Test]
        public void MetalTotalGetterTest()
        {
            Tf2Value value = Tf2Value.Refined + Tf2Value.Reclaimed * 2; //1.66 ref
            Assert.AreEqual(5.0 / 3, value.RefinedTotal, 0.01);
            Assert.AreEqual(5, value.ReclaimedTotal);
            Assert.AreEqual(15, value.ScrapTotal);
        }

        [Test]
        public void MetalTotalGetterTest2()
        {
            Tf2Value value = Tf2Value.Scrap; //0.11 ref
            Assert.AreEqual(1.0 / 9, value.RefinedTotal, 0.01);
            Assert.AreEqual(1.0 / 3, value.ReclaimedTotal, 0.01);
            Assert.AreEqual(1, value.ScrapTotal);
        }

        [Test]
        public void GrainTotalGetterTest2()
        {
            Assert.AreEqual(Tf2Value.Refined.GrainTotal, Tf2Value.Scrap.GrainTotal * 9);
            Assert.AreEqual(Tf2Value.Refined.GrainTotal, Tf2Value.Reclaimed.GrainTotal * 3);
        }

        [Test]
        public void GetItemPartTest()
        {
            Tf2Value keyPrice = 10 * Tf2Value.Refined;
            Tf2Value remainder;
            Assert.AreEqual(2, (25 * Tf2Value.Refined).GetPriceUsingItem(keyPrice, out remainder));
            Assert.AreEqual(5 * Tf2Value.Refined, remainder);
        }

        [Test]
        public void GetItemPartNoKeysTest()
        {
            Tf2Value keyPrice = 10 * Tf2Value.Refined;
            Tf2Value remainder;
            Assert.AreEqual(0, (9 * Tf2Value.Refined).GetPriceUsingItem(keyPrice, out remainder));
            Assert.AreEqual(9 * Tf2Value.Refined, remainder);
        }

        [Test]
        public void GetItemPartNoRemainderTest()
        {
            Tf2Value keyPrice = 10 * Tf2Value.Refined;
            Tf2Value remainder;
            Assert.AreEqual(2, (20 * Tf2Value.Refined).GetPriceUsingItem(keyPrice, out remainder));
            Assert.AreEqual(Tf2Value.Zero, remainder);
        }

        [Test]
        public void GetItemPartWorksWhenPassingSelfTest()
        {
            Tf2Value keyPrice = 10 * Tf2Value.Refined;
            Tf2Value value = 25 * Tf2Value.Refined;
            Assert.AreEqual(2, value.GetPriceUsingItem(keyPrice, out value));
            Assert.AreEqual(5 * Tf2Value.Refined, value);
        }

        [Test]
        public void GetItemTotalTest()
        {
            Tf2Value keyPrice = 10 * Tf2Value.Refined;
            Assert.AreEqual(2.5, (25 * Tf2Value.Refined).GetPriceUsingItem(keyPrice));
        }

        [Test]
        public void GetItemTotalNoKeysTest()
        {
            Tf2Value keyPrice = 10 * Tf2Value.Refined;
            Assert.AreEqual(0.5, (5 * Tf2Value.Refined).GetPriceUsingItem(keyPrice));
        }

        [Test]
        public void GetItemTotalNoDecimalTest()
        {
            Tf2Value keyPrice = 10 * Tf2Value.Refined;
            Assert.AreEqual(2, (20 * Tf2Value.Refined).GetPriceUsingItem(keyPrice));
        }

        [Test]
        public void GetRefTotalStringTest()
        {
            Assert.AreEqual("0.11 ref", (Tf2Value.Scrap * 1).ToString());
            Assert.AreEqual("0.22 ref", (Tf2Value.Scrap * 2).ToString());
            Assert.AreEqual("0.33 ref", (Tf2Value.Scrap * 3).ToString());
            Assert.AreEqual("0.44 ref", (Tf2Value.Scrap * 4).ToString());
            Assert.AreEqual("0.55 ref", (Tf2Value.Scrap * 5).ToString());
            Assert.AreEqual("0.66 ref", (Tf2Value.Scrap * 6).ToString());
            Assert.AreEqual("0.77 ref", (Tf2Value.Scrap * 7).ToString());
            Assert.AreEqual("0.88 ref", (Tf2Value.Scrap * 8).ToString());
            Assert.AreEqual("1 ref", (Tf2Value.Scrap * 9).ToString());
            Assert.AreEqual("1.11 ref", (Tf2Value.Scrap * 10).ToString());
            Assert.AreEqual("1.22 ref", (Tf2Value.Scrap * 11).ToString());
            Assert.AreEqual("1.33 ref", (Tf2Value.Scrap * 12).ToString());
            Assert.AreEqual("1.44 ref", (Tf2Value.Scrap * 13).ToString());
            Assert.AreEqual("1.55 ref", (Tf2Value.Scrap * 14).ToString());
            Assert.AreEqual("1.66 ref", (Tf2Value.Scrap * 15).ToString());
            Assert.AreEqual("1.77 ref", (Tf2Value.Scrap * 16).ToString());
            Assert.AreEqual("1.88 ref", (Tf2Value.Scrap * 17).ToString());
            Assert.AreEqual("2 ref", (Tf2Value.Scrap * 18).ToString());

            Assert.AreEqual("100 ref", (Tf2Value.Refined * 100).ToString());
        }

        [Test]
        public void GetRefPartsStringTest()
        {
            Assert.AreEqual("1 scrap", (Tf2Value.Scrap * 1).ToPartsString());
            Assert.AreEqual("2 scrap", (Tf2Value.Scrap * 2).ToPartsString());
            Assert.AreEqual("1 rec", (Tf2Value.Scrap * 3).ToPartsString());
            Assert.AreEqual("1 rec + 1 scrap", (Tf2Value.Scrap * 4).ToPartsString());
            Assert.AreEqual("1 rec + 2 scrap", (Tf2Value.Scrap * 5).ToPartsString());
            Assert.AreEqual("2 rec", (Tf2Value.Scrap * 6).ToPartsString());
            Assert.AreEqual("2 rec + 1 scrap", (Tf2Value.Scrap * 7).ToPartsString());
            Assert.AreEqual("2 rec + 2 scrap", (Tf2Value.Scrap * 8).ToPartsString());
            Assert.AreEqual("1 ref", (Tf2Value.Scrap * 9).ToPartsString());
            Assert.AreEqual("1 ref + 1 scrap", (Tf2Value.Scrap * 10).ToPartsString());
            Assert.AreEqual("1 ref + 2 scrap", (Tf2Value.Scrap * 11).ToPartsString());
            Assert.AreEqual("1 ref + 1 rec", (Tf2Value.Scrap * 12).ToPartsString());
            Assert.AreEqual("1 ref + 1 rec + 1 scrap", (Tf2Value.Scrap * 13).ToPartsString());
            Assert.AreEqual("1 ref + 1 rec + 2 scrap", (Tf2Value.Scrap * 14).ToPartsString());
            Assert.AreEqual("1 ref + 2 rec", (Tf2Value.Scrap * 15).ToPartsString());
            Assert.AreEqual("1 ref + 2 rec + 1 scrap", (Tf2Value.Scrap * 16).ToPartsString());
            Assert.AreEqual("1 ref + 2 rec + 2 scrap", (Tf2Value.Scrap * 17).ToPartsString());
            Assert.AreEqual("2 ref", (Tf2Value.Scrap * 18).ToPartsString());

            Assert.AreEqual("100 ref", (Tf2Value.Refined * 100).ToPartsString());
        }

        [Test]
        public void FromRefTest()
        {
            Assert.AreEqual(1, Tf2Value.FromRef(0.11).ScrapTotal);
            Assert.AreEqual(2, Tf2Value.FromRef(0.22).ScrapTotal);
            Assert.AreEqual(3, Tf2Value.FromRef(0.33).ScrapTotal);
            Assert.AreEqual(4, Tf2Value.FromRef(0.44).ScrapTotal);
            Assert.AreEqual(5, Tf2Value.FromRef(0.55).ScrapTotal);
            Assert.AreEqual(6, Tf2Value.FromRef(0.66).ScrapTotal);
            Assert.AreEqual(7, Tf2Value.FromRef(0.77).ScrapTotal);
            Assert.AreEqual(8, Tf2Value.FromRef(0.88).ScrapTotal);
            Assert.AreEqual(9, Tf2Value.FromRef(1).ScrapTotal);
            Assert.AreEqual(10, Tf2Value.FromRef(1.11).ScrapTotal);
            Assert.AreEqual(11, Tf2Value.FromRef(1.22).ScrapTotal);
            Assert.AreEqual(12, Tf2Value.FromRef(1.33).ScrapTotal);
            Assert.AreEqual(20, Tf2Value.FromRef(2.22).ScrapTotal);

            Assert.AreEqual(9000, Tf2Value.FromRef(1000).ScrapTotal);
            Assert.AreEqual(9001, Tf2Value.FromRef(1000.11).ScrapTotal);
        }

        [Test]
        public void FromRefStringTest()
        {
            Assert.AreEqual(1, Tf2Value.FromRef("0.11").ScrapTotal);
            Assert.AreEqual(2, Tf2Value.FromRef("0.22").ScrapTotal);
            Assert.AreEqual(3, Tf2Value.FromRef("0.33").ScrapTotal);
            Assert.AreEqual(4, Tf2Value.FromRef("0.44").ScrapTotal);
            Assert.AreEqual(5, Tf2Value.FromRef("0.55").ScrapTotal);
            Assert.AreEqual(6, Tf2Value.FromRef("0.66").ScrapTotal);
            Assert.AreEqual(7, Tf2Value.FromRef("0.77").ScrapTotal);
            Assert.AreEqual(8, Tf2Value.FromRef("0.88").ScrapTotal);
            Assert.AreEqual(9, Tf2Value.FromRef("1").ScrapTotal);
            Assert.AreEqual(10, Tf2Value.FromRef("1.11").ScrapTotal);
            Assert.AreEqual(11, Tf2Value.FromRef("1.22").ScrapTotal);
            Assert.AreEqual(12, Tf2Value.FromRef("1.33").ScrapTotal);
            Assert.AreEqual(20, Tf2Value.FromRef("2.22").ScrapTotal);
        }

        [Test]
        public void FromRefStringThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => Tf2Value.FromRef(null));
            Assert.Throws<FormatException>(() => Tf2Value.FromRef("eleven"));
        }

        [Test]
        public void DifferenceTest()
        {
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Difference(Tf2Value.Refined, Tf2Value.Refined));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Difference(Tf2Value.Scrap * 8, Tf2Value.Scrap * 9));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Difference(Tf2Value.Scrap * 9, Tf2Value.Scrap * 8));
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Difference(Tf2Value.Scrap, Tf2Value.Scrap + Tf2Value.Refined));
        }

        [Test]
        public void MaxTest()
        {
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Max(Tf2Value.Refined, Tf2Value.Refined));
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Max(Tf2Value.Refined, Tf2Value.Scrap));
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Max(Tf2Value.Scrap, Tf2Value.Refined));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Max(Tf2Value.Scrap, Tf2Value.Zero));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Max(Tf2Value.Zero, Tf2Value.Scrap));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Max(Tf2Value.Scrap, Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Grain, Tf2Value.Max(Tf2Value.Zero, Tf2Value.Grain));
        }

        [Test]
        public void MinTest()
        {
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Min(Tf2Value.Refined, Tf2Value.Refined));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Min(Tf2Value.Refined, Tf2Value.Scrap));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Min(Tf2Value.Scrap, Tf2Value.Refined));
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Min(Tf2Value.Scrap, Tf2Value.Zero));
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Min(Tf2Value.Zero, Tf2Value.Refined));
            Assert.AreEqual(Tf2Value.Grain, Tf2Value.Min(Tf2Value.Scrap, Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Min(Tf2Value.Zero, Tf2Value.Grain));
        }

        [Test]
        public void CeilingTest()
        {
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Ceiling(Tf2Value.Zero));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Ceiling(Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Ceiling(Tf2Value.Scrap));
            Assert.AreEqual(Tf2Value.Reclaimed, Tf2Value.Ceiling(Tf2Value.Reclaimed));
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Ceiling(Tf2Value.Refined));

            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Ceiling(Tf2Value.Scrap / 2));
            Assert.AreEqual(Tf2Value.Refined + Tf2Value.Scrap, Tf2Value.Ceiling(Tf2Value.Refined + Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Refined + Tf2Value.Scrap, Tf2Value.Ceiling(Tf2Value.Refined + Tf2Value.Scrap));
        }

        [Test]
        public void FloorTest()
        {
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Floor(Tf2Value.Zero));
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Floor(Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Floor(Tf2Value.Scrap));
            Assert.AreEqual(Tf2Value.Reclaimed, Tf2Value.Floor(Tf2Value.Reclaimed));
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Floor(Tf2Value.Refined));

            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Floor(Tf2Value.Scrap / 2));
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Floor(Tf2Value.Refined + Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Refined + Tf2Value.Scrap, Tf2Value.Floor(Tf2Value.Refined + Tf2Value.Scrap));
        }

        [Test]
        public void RoundTest()
        {
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Round(Tf2Value.Zero));
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Round(Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Round(Tf2Value.Scrap));
            Assert.AreEqual(Tf2Value.Reclaimed, Tf2Value.Round(Tf2Value.Reclaimed));
            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Round(Tf2Value.Refined));

            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Round(Tf2Value.Scrap / 2 - Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Round(Tf2Value.Scrap / 2));
            Assert.AreEqual(Tf2Value.Scrap, Tf2Value.Round(Tf2Value.Scrap / 2 + Tf2Value.Grain));

            Assert.AreEqual(Tf2Value.Refined, Tf2Value.Round(Tf2Value.Refined + Tf2Value.Scrap / 2 - Tf2Value.Grain));
            Assert.AreEqual(Tf2Value.Refined + Tf2Value.Scrap, Tf2Value.Round(Tf2Value.Refined + Tf2Value.Scrap / 2));
            Assert.AreEqual(Tf2Value.Refined + Tf2Value.Scrap, Tf2Value.Round(Tf2Value.Refined + Tf2Value.Scrap / 2 + Tf2Value.Grain));
        }

        [Test]
        public void SubtrationTest()
        {
            Assert.AreEqual(Tf2Value.Reclaimed * 2, Tf2Value.Refined - Tf2Value.Reclaimed);
            Assert.AreEqual(Tf2Value.Zero, Tf2Value.Refined - Tf2Value.Refined);
        }

        [Test]
        public void NegativeValueThrowsTest()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Tf2Value value = Tf2Value.Scrap - Tf2Value.Reclaimed;
            });

            Assert.Throws<ArgumentException>(() =>
            {
                Tf2Value value = Tf2Value.Reclaimed - Tf2Value.Refined;
            });
        }

        [Test]
        public void DivisionTest()
        {
            Assert.AreEqual(Tf2Value.Scrap, (Tf2Value.Scrap * 3) / 3);
            Assert.AreEqual(3, (Tf2Value.Scrap * 3) / Tf2Value.Scrap);
        }

        [Test]
        public void ModulusTest()
        {
            Assert.AreEqual(Tf2Value.Scrap, (2 * Tf2Value.Refined + Tf2Value.Scrap) % Tf2Value.Refined);
        }
    }
}
