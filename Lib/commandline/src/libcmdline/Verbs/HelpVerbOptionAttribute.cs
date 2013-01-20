﻿#region License
//
// Command Line Library: HelpVerbOptionAttribute.cs
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
    /// Indicates the instance method that must be invoked when it becomes necessary show your help screen.
    /// The method signature is an instance method with that accepts and returns a <see cref="System.String"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class HelpVerbOptionAttribute : BaseOptionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.HelpVerbOptionAttribute"/> class.
        /// Although it is possible, it is strongly discouraged redefine the long name for this option
        /// not to disorient your users.
        /// </summary>
        public HelpVerbOptionAttribute()
            : this("help")
        {
            HelpText = DefaultHelpText;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine.HelpVerbOptionAttribute"/> class
        /// with the specified long name. Use parameterless constructor instead.
        /// </summary>
        /// <param name="longName"></param>
        public HelpVerbOptionAttribute(string longName)
        {
            LongName = longName;
            HelpText = DefaultHelpText;
        }

        /// <summary>
        /// Help verb command do not support short name by design.
        /// </summary>
        public override char? ShortName
        {
            get { return null; }
            internal set { throw new InvalidOperationException("Help verb command do not support short name by design."); }
        }

        /// <summary>
        /// Help verb command like ordinary help option cannot be mandatory by design.
        /// </summary>
        public override bool Required
        {
            get { return false; }
            set { throw new InvalidOperationException("Help verb command cannot be mandatory by design."); }
        }

        internal static void InvokeMethod(object target,
            Pair<MethodInfo, HelpVerbOptionAttribute> helpInfo, string verb, out string text)
        {
            text = null;
            var method = helpInfo.Left;
            if (!CheckMethodSignature(method))
            {
                throw new MemberAccessException(string.Format(
                    "{0} has an incorrect signature. " +
                    "Help verb command requires a method that accepts and returns a string.", method.Name));
            }
            text = (string) method.Invoke(target, new object[] {verb});
        }

        private static bool CheckMethodSignature(MethodInfo value)
        {
            if (value.ReturnType == typeof(string) && value.GetParameters().Length == 1)
            {
                return value.GetParameters()[0].ParameterType == typeof(string);
            }
            return false;
        }

        private const string DefaultHelpText = "Display more information on a specific command.";
    }
}
#endif