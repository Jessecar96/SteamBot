﻿#region License
//
// Command Line Library: CommandLine.cs
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
    /// Models an option that can accept multiple values as separated arguments.
    /// </summary>
    public sealed class OptionArrayAttribute : OptionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionArrayAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        public OptionArrayAttribute(char shortName) : base(shortName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionArrayAttribute"/> class.
        /// </summary>
        /// <param name="longName">The long name of the option.</param>
        public OptionArrayAttribute(string longName) : base(longName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.OptionArrayAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionArrayAttribute(char shortName, string longName) : base(shortName, longName) { }
    }
}