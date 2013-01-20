﻿#region License
//
// Command Line Library: VerbOptionAttribute.cs
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
#region Preprocessor Directives
// Comment this line if you want disable support for verb commands.
// Here the symbol is provided for unit tests convenience, if you don't need
// this feature please exclude this file from your source tree.
#define CMDLINE_VERBS
#endregion
#if CMDLINE_VERBS
#region Using Directives
using System;
using System.Collections.Generic;
using System.Reflection;
using CommandLine.Internal;
#endregion
//
// Needs CMDLINE_VERBS preprocessor directive uncommented in CommandLine.cs.
//
namespace CommandLine
{
    /// <summary>
    /// Models a verb command specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class VerbOptionAttribute : OptionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.VerbOptionAttribute"/> class.
        /// </summary>
        /// <param name="longName">The long name of the verb command.</param>
        public VerbOptionAttribute(string longName)
            : base(longName)
        {
            Assumes.NotNullOrEmpty(longName, "longName");
        }

        /// <summary>
        /// Verb commands do not support short name by design.
        /// </summary>
        public override char? ShortName
        {
            get { return null; }
            internal set {}
        }

        /// <summary>
        /// Verb commands cannot be mandatory since are mutually exclusive by design.
        /// </summary>
        public override bool Required
        {
            get { return false; }
            set {}
        }
    }
}
#endif