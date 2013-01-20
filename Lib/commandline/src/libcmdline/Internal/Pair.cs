﻿#region License
//
// Command Line Library: Pair.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
#endregion

namespace CommandLine.Internal
{
    internal sealed class Pair<TLeft, TRight>
        where TLeft : class
        where TRight : class
    {
        public Pair(TLeft left, TRight right)
        {
            _left = left;
            _right = right;
        }

        public TLeft Left
        {
            get { return _left; }
        }

        public TRight Right
        {
            get { return _right; }
        }

        public override int GetHashCode()
        {
            int leftHash = (_left == null ? 0 : _left.GetHashCode());
            int rightHash = (_right == null ? 0 : _right.GetHashCode());

            return leftHash ^ rightHash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Pair<TLeft, TRight>;

            if (other == null)
            {
                return false;
            }
            return Equals(_left, other._left) && Equals(_right, other._right);
        }

        private readonly TLeft _left;
        private readonly TRight _right;
    }
}