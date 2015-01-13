using System;
using System.Collections.Generic;

namespace SteamTrade
{
    /// <summary>
    /// An immutable structure representing some amount of TF2 metal.  Values can be added, subtracted, etc.
    /// Negative values are not allowed.  Attempting to do, for instance, Tf2Value.Zero - Tf2Value.Scrap will throw an exception.
    /// </summary>
    public struct Tf2Value : IEquatable<Tf2Value>, IComparable<Tf2Value>, IComparable
    {
        private const int GRAIN_PER_SCRAP = 840;
        private const int GRAIN_PER_RECLAIMED = GRAIN_PER_SCRAP * SCRAP_PER_RECLAIMED;
        private const int GRAIN_PER_REFINED = GRAIN_PER_SCRAP * SCRAP_PER_REFINED;
        private const int SCRAP_PER_RECLAIMED = 3;
        private const int SCRAP_PER_REFINED = SCRAP_PER_RECLAIMED * RECLAIMED_PER_REFINED;
        private const int RECLAIMED_PER_REFINED = 3;

        /// <summary>
        /// A grain is a made-up type of metal, allowing us to create values equal to fractions of a scrap.
        /// Usually you will not need to work with these directly.
        /// To get the number of Grain per Scrap, use Tf2Value.Scrap.GrainTotal
        /// </summary>
        public static readonly Tf2Value Grain = new Tf2Value(1);

        public static readonly Tf2Value Zero = new Tf2Value(0);
        public static readonly Tf2Value Scrap = Grain * GRAIN_PER_SCRAP;
        public static readonly Tf2Value Reclaimed = Grain * GRAIN_PER_RECLAIMED;
        public static readonly Tf2Value Refined = Grain * GRAIN_PER_REFINED;

        private readonly int _numGrains;

        /// <summary>
        /// Creates a new Tf2Value from the given number of grains
        /// </summary>
        private Tf2Value(int numGrains)
        {
            if (numGrains < 0)
                throw new ArgumentException("Cannot create a Tf2Value with negative value");
            _numGrains = numGrains;
        }

        #region Getters for scrap/refined/etc

        /// <summary>
        /// The total overall scrap.
        /// Example: if the value is 3 ref + 2 rec + 1 scrap, the ScrapTotal would be 3*9 + 2*3 + 1 = 34
        /// </summary>
        public double ScrapTotal { get { return (double)_numGrains/GRAIN_PER_SCRAP; } }

        /// <summary>
        /// Only the scrap portion of this Tf2Value.
        /// Example: if the value is 3 ref + 2 rec + 1 scrap, the ScrapPart would be 1
        /// </summary>
        public int ScrapPart { get { return ((int)ScrapTotal) % SCRAP_PER_RECLAIMED; } }

        /// <summary>
        /// The total overall reclaimed.
        /// Example: if the value is 3 ref + 2 rec + 1 scrap, the ReclaimedTotal would be 3*3 + 2 + 1/3 = 11.3333..
        /// </summary>
        public double ReclaimedTotal { get { return (double) _numGrains/GRAIN_PER_RECLAIMED; } }

        /// <summary>
        /// Only the reclaimed portion of this Tf2Value.
        /// Example: if the value is 3 ref + 2 rec + 1 scrap, the ReclaimedPart would be 2
        /// </summary>
        public int ReclaimedPart { get { return ((int)ReclaimedTotal) % RECLAIMED_PER_REFINED; } }

        /// <summary>
        /// The total overall refined.
        /// Example: if the value is 3 ref + 2 rec + 1 scrap, the RefinedTotal would be 3 + 2/3 + 1/9 = 3.7777..
        /// </summary>
        public double RefinedTotal { get { return (double)_numGrains / GRAIN_PER_REFINED; } }

        /// <summary>
        /// Only the reclaimed portion of this Tf2Value.
        /// Example: if the value is 3 ref + 2 rec + 1 scrap, the RefinedPart would be 3
        /// </summary>
        public int RefinedPart { get { return (int)RefinedTotal; } }

        /// <summary>
        /// A helper-property to get the value after the decimal-point for a refined-string.
        /// Example: the value 3 ref + 2 rec + 1 scrap is commonly written "3.77 ref", so RefinedPartDecimal = 77.
        /// If you're just looking for the string "3.77 ref", call ToRefString() instead
        /// </summary>
        public int RefinedPartDecimal { get { return 11 * (ScrapPart + 3 * ReclaimedPart); } }

        /// <summary>
        /// The total number of grains.
        /// See the documentation for Tf2Value.Grain for an explaination of grains.
        /// There are very few cases where you will need to use this.
        /// </summary>
        public int GrainTotal { get { return _numGrains; } }

        /// <summary>
        /// Only the grain portion of this Tf2Value.
        /// See the documentation for Tf2Value.Grain for an explaination of grains.
        /// There are very few cases where you will need to use this.
        /// </summary>
        public int GrainPart { get { return _numGrains % GRAIN_PER_SCRAP; } }

        /// <summary>
        /// Returns the item-price portion of this Tf2Value.
        /// Example: If keyPrice = 10 ref, then
        /// (25*Tf2Value.Refined).GetItemPart(keyPrice, out remainder) == 2
        /// with remainder == 5 ref
        /// </summary>
        /// <param name="itemValue">The value of the item (eg. the current key-price)</param>
        /// <param name="remainder">How much is leftover</param>
        public int GetItemPart(Tf2Value itemValue, out Tf2Value remainder)
        {
            int numItems = (int) (this/itemValue); //Calculate value first in case remainder = this
            remainder = this % itemValue;
            return numItems;
        }

        /// <summary>
        /// Returns the item-price portion of this Tf2Value.
        /// Example: If keyPrice = 10 ref, then
        /// (25*Tf2Value.Refined).GetItemPart(keyPrice, out remainder) == 2.5
        /// with remainder == 5 ref
        /// </summary>
        /// <param name="itemValue">The value of the item (eg. the current key-price)</param>
        public double GetItemTotal(Tf2Value itemValue)
        {
            return this / itemValue;
        }

        #endregion

        #region Creation methods
        /// <summary>
        /// Creates a Tf2Value equal to the given number of ref, rounded to the nearest scrap.
        /// The rounding is done so that, for instance, "1.11" ref is equal to 10 scrap, even
        /// though 10 scrap is actually "1.11111..."
        /// </summary>
        public static Tf2Value FromRef(double numRef)
        {
            return Round(numRef*Refined);
        }

        /// <summary>
        /// Creates a Tf2Value equal to the given number of ref, rounded to the nearest scrap.
        /// The rounding is done so that, for instance, "1.11" ref is equal to 10 scrap, even
        /// though 10 scrap is actually "1.11111..."
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if argument is null</exception>
        /// <exception cref="FormatException">Thrown if string is not a valid number</exception>
        public static Tf2Value FromRef(string numRefStr)
        {
            return FromRef(double.Parse(numRefStr));
        }
        #endregion

        #region String methods

        /// <summary>
        /// Returns a string displaying how many of a certain item this Tf2Value represents.
        /// Example: if the value of a TF2 key is tf2KeyValue, then
        /// Tf2Value someValue = tf2KeyValue + Tf2Value.Refined;
        /// someValue.ToItemString(tf2KeyValue, "key")
        /// returns
        /// "1 key + 1 ref"
        /// </summary>
        /// <param name="itemValue">The value of the item (eg. the current key-price)</param>
        /// <param name="itemName">The name of the item.</param>
        /// <param name="itemNamePlural">By default, to make plural strings we attach 's' to the end.
        ///   This parameter lets you set a different plural.</param>
        /// <param name="roundDown">If true, only the item portion of the value will be shown.
        ///   If false, the portion in ref will be shown too</param>
        public string ToItemString(Tf2Value itemValue, string itemName, string itemNamePlural = null, bool roundDown = false)
        {
            if (String.IsNullOrEmpty(itemNamePlural))
            {
                itemNamePlural = itemName + "s";
            }

            if(_numGrains == 0 || (roundDown && this < itemValue))
            {
                return "0 " + itemNamePlural;
            }

            int numItems = (int)(this/itemValue);
            Tf2Value leftovers = this%itemValue;

            string returnValue = "";
            if(numItems > 0)
            {
                returnValue += String.Format("{0} {1}", numItems, numItems > 1 ? itemNamePlural : itemName);
            }

            if(leftovers > Zero && !roundDown)
            {
                if(numItems > 0)
                {
                    returnValue += " + ";
                }
                returnValue += leftovers.ToRefString();
            }

            return returnValue;
        }

        /// <summary>
        /// Returns a string displaying how many total refined metal this Tf2Value represents
        /// Example: For the value 3 ref + 2 rec + 1 scrap, this method returns "3.77 ref"
        /// </summary>
        public string ToRefString()
        {
            return String.Format("{0}{1} ref", RefinedPart, (RefinedPartDecimal > 0 ? "." + RefinedPartDecimal : ""));
        }

        /// <summary>
        /// Returns a string displaying how much metal this Tf2Value represents, broken into parts
        /// Example: For the value 3 ref + 2 rec + 1 scrap, this method returns "3 ref + 2 rec + 1 scrap"
        /// </summary>
        /// <param name="includeScrapFractions">
        /// If true, fractions of a scrap are included in the output (up to two decimal places)
        /// If false, the value is rounded down to the nearest scrap.
        /// Default is false.
        /// </param>
        public string ToPartsString(bool includeScrapFractions = false)
        {
            if(_numGrains == 0)
                return "0 ref";

            List<string> parts = new List<string>();
            if(RefinedPart > 0)
                parts.Add(RefinedPart + " ref");
            if(ReclaimedPart > 0)
                parts.Add(ReclaimedPart + " rec");
            
            //Scrap-case is somewhat special
            int reclaimedInScrap = (int) ReclaimedTotal*SCRAP_PER_RECLAIMED;
            double scrapRemaining = ScrapTotal - reclaimedInScrap;
            if (scrapRemaining > 0)
            {
                string toStringArgument = (includeScrapFractions ? "0.##" : "0");
                string numScrapString = scrapRemaining.ToString(toStringArgument);
                parts.Add(numScrapString + " scrap");
            }

            return string.Join(" + ", parts);
        }

        public override string ToString()
        {
            return ToRefString();
        }
        #endregion

        #region Math stuff
        /// <summary>
        /// Returns the difference in value between two Tf2Values.
        /// This is different from operator- in that the result is never negative
        /// (and thus never throws an exception, since Tf2Values can't be negative)
        /// </summary>
        public static Tf2Value Difference(Tf2Value val1, Tf2Value val2)
        {
            return new Tf2Value(Math.Abs(val1._numGrains - val2._numGrains));
        }

        /// <summary>
        /// Returns the maximum of two TF2Values
        /// Example: Tf2Value.Max(Tf2Value.Scrap, Tf2Value.Refined) returns Tf2Value.Refined
        /// </summary>
        public static Tf2Value Max(Tf2Value val1, Tf2Value val2)
        {
            return (val1 > val2 ? val1 : val2);
        }

        /// <summary>
        /// Returns the minimum of two TF2Values
        /// Example: Tf2Value.Max(Tf2Value.Scrap, Tf2Value.Refined) returns Tf2Value.Scrap
        /// </summary>
        public static Tf2Value Min(Tf2Value val1, Tf2Value val2)
        {
            return (val1 < val2 ? val1 : val2);
        }

        /// <summary>
        /// Round this TF2Value up to the nearest scrap
        /// Example: Tf2Value.Ceiling(Tf2Value.Refined / 2) = 5 scrap
        /// </summary>
        public static Tf2Value Ceiling(Tf2Value value)
        {
            return new Tf2Value((int)Math.Ceiling(value.ScrapTotal) * GRAIN_PER_SCRAP);
        }

        /// <summary>
        /// Round this TF2Value down to the nearest scrap
        /// Example: Tf2Value.Floor(Tf2Value.Refined / 2) = 4 scrap
        /// </summary>
        public static Tf2Value Floor(Tf2Value value)
        {
            return new Tf2Value((int)Math.Floor(value.ScrapTotal) * GRAIN_PER_SCRAP);
        }

        /// <summary>
        /// Round this TF2Value to the nearest scrap.
        /// By default, 0.5 is always rounded up.  Note that this is different from the default behavior for
        /// Math.Round(), because its default is stupid.
        /// Example: Tf2Value.Round(Tf2Value.Refined / 2) = 5 scrap
        /// </summary>
        public static Tf2Value Round(Tf2Value value, MidpointRounding roundingRule = MidpointRounding.AwayFromZero)
        {
            return new Tf2Value((int) Math.Round(value.ScrapTotal, roundingRule)*GRAIN_PER_SCRAP);
        }

        #endregion

        #region Custom operators
        public static bool operator ==(Tf2Value val1, Tf2Value val2)
        {
            return (val1._numGrains == val2._numGrains);
        }

        public static bool operator !=(Tf2Value val1, Tf2Value val2)
        {
            return (val1._numGrains != val2._numGrains);
        }

        public static bool operator >(Tf2Value val1, Tf2Value val2)
        {
            return (val1._numGrains > val2._numGrains);
        }

        public static bool operator <(Tf2Value val1, Tf2Value val2)
        {
            return (val1._numGrains < val2._numGrains);
        }

        public static bool operator >=(Tf2Value val1, Tf2Value val2)
        {
            return (val1._numGrains >= val2._numGrains);
        }

        public static bool operator <=(Tf2Value val1, Tf2Value val2)
        {
            return (val1._numGrains <= val2._numGrains);
        }

        public static Tf2Value operator +(Tf2Value val1, Tf2Value val2)
        {
            return new Tf2Value(val1._numGrains + val2._numGrains);
        }

        public static Tf2Value operator -(Tf2Value val1, Tf2Value val2)
        {
            return new Tf2Value(val1._numGrains - val2._numGrains);
        }

        public static Tf2Value operator *(Tf2Value val1, double val2)
        {
            return new Tf2Value((int)(val1._numGrains * val2));
        }

        public static Tf2Value operator *(double val1, Tf2Value val2)
        {
            return new Tf2Value((int)(val1 * val2._numGrains));
        }

        public static double operator /(Tf2Value val1, Tf2Value val2)
        {
            return (double)val1._numGrains / val2._numGrains;
        }

        public static Tf2Value operator /(Tf2Value val1, double val2)
        {
            return new Tf2Value((int)(val1._numGrains / val2));
        }

        public static Tf2Value operator %(Tf2Value val1, Tf2Value val2)
        {
            return new Tf2Value(val1._numGrains % val2._numGrains);
        }
        #endregion

        #region IEquatable/IComparable
        public bool Equals(Tf2Value other)
        {
            return _numGrains == other._numGrains;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj) || !(obj is Tf2Value))
                return false;
            return Equals((Tf2Value)obj);
        }

        public override int GetHashCode()
        {
            return _numGrains.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj) || !(obj is Tf2Value))
                return 1;
            return CompareTo((Tf2Value)obj);
        }

        public int CompareTo(Tf2Value other)
        {
            return _numGrains.CompareTo(other._numGrains);
        }
        #endregion
    }
}
