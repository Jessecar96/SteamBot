#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Newtonsoft.Json.Utilities
{
  internal class MathUtils
  {
    public static int IntLength(int i)
    {
      if (i < 0)
        throw new ArgumentOutOfRangeException();

      if (i == 0)
        return 1;

      return (int)Math.Floor(Math.Log10(i)) + 1;
    }

    public static char IntToHex(int n)
    {
      if (n <= 9)
        return (char)(n + 48);

      return (char)((n - 10) + 97);
    }

    public static int? Min(int? val1, int? val2)
    {
      if (val1 == null)
        return val2;
      if (val2 == null)
        return val1;

      return Math.Min(val1.Value, val2.Value);
    }

    public static int? Max(int? val1, int? val2)
    {
      if (val1 == null)
        return val2;
      if (val2 == null)
        return val1;

      return Math.Max(val1.Value, val2.Value);
    }

    public static double? Max(double? val1, double? val2)
    {
      if (val1 == null)
        return val2;
      if (val2 == null)
        return val1;

      return Math.Max(val1.Value, val2.Value);
    }

    public static bool ApproxEquals(double d1, double d2)
    {
      const double epsilon = 2.2204460492503131E-16;

      if (d1 == d2)
        return true;

      double tolerance = ((Math.Abs(d1) + Math.Abs(d2)) + 10.0) * epsilon;
      double difference = d1 - d2;

      return (-tolerance < difference && tolerance > difference);
    }
  }
}