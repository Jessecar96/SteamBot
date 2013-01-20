#region License
//
// Command Line Library: CommandLineParserBaseFixture.cs
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
#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Should.Fluent;
#endregion

namespace CommandLine.Tests
{
    public abstract class CommandLineParserBaseFixture : BaseFixture
    {
        protected CommandLineParserBaseFixture()
        {
            // Before latest changes, some values were parsed with CultureInfo.InvariantCulture
            // that is compatible with en-US.
            // Following instructions prevent old tests from break.
            // New tests were added for verify new culture-specific support.
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }

        protected bool? Result { set; get; }

        [SetUp]
        public virtual void CreateInstance()
        {
            Parser = new CommandLineParser();
        }

        protected void ResultShouldBeTrue()
        {
            Result.Should().Be.True();
            Result = null;
        }

        protected void ResultShouldBeFalse()
        {
            Result.Should().Be.False();
            Result = null;
        }

        protected ICommandLineParser Parser { get; set; }
    }
}