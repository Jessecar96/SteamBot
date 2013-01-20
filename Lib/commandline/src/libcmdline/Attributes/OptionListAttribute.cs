﻿#region License
//
// Command Line Library: OptionListAttribute.cs
//
// Author:
//   Giacomo Stelluti Scala (gsscoder@gmail.com)
//
// Copyright (C) 2005 - 2013 Giacomo Stelluti Scala
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace CommandLine
{
    /// <summary>
    /// Models an option that can accept multiple values.
    /// Must be applied to a field compatible with an <see cref="System.Collections.Generic.IList&lt;T&gt;"/> interface
    /// of <see cref="System.String"/> instances.
    /// </summary>
    public sealed class OptionListAttribute : OptionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionListAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        public OptionListAttribute(char shortName) : base(shortName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionListAttribute"/> class.
        /// </summary>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionListAttribute(string longName) : base(longName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionListAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionListAttribute(char shortName, string longName)
            : base(longName)
        {
            Separator = ':';
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionListAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the option or null if not used.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        /// <param name="separator">Values separator character.</param>
        public OptionListAttribute(char shortName, string longName, char separator)
            : base(shortName, longName)
        {
            Separator = separator;
        }

        /// <summary>
        /// Gets or sets the values separator character.
        /// </summary>
        public char Separator { get; set; }
    }
}