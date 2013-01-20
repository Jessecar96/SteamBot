﻿#region License
//
// Command Line Library: SimpleOptionsWithValueList.cs
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
#if UNIT_TESTS_VERBS
#region Using Directives
using System.Collections.Generic;
using System.ComponentModel;
using CommandLine.Text;

#endregion

namespace CommandLine.Tests.Mocks
{
    abstract class CommonSubOptions
    {
        [Option('q', "quiet",
            HelpText = "Suppress summary message.")]
        public bool Quiet { get; set; }

        public int? CreationProof { get; set; }
    }

    class AddSubOptions : CommonSubOptions
    {
        [Option('p', "patch", MutuallyExclusiveSet = "mode",
            HelpText = "Interactively choose hunks of patch between the index and the work tree and add them to the index.")]
        public bool Patch { get; set; }

        [Option('f', "force", MutuallyExclusiveSet = "mode",
            HelpText = "Allow adding otherwise ignored files.")]
        public bool Force { get; set; }

        [ValueList(typeof(List<string>), MaximumElements = 1)]
        public IList<string> FileName { get; set; }
    }

    class CommitSubOptions : CommonSubOptions
    {
        [Option('p', "patch",
            HelpText = "Use the interactive patch selection interface to chose which changes to commit.")]
        public bool Patch { get; set; }

        [Option("amend", HelpText = "Used to amend the tip of the current branch.")]
        public bool Amend { get; set; }
    }

    class CloneSubOptions : CommonSubOptions
    {
        [Option("no-hardlinks",
            HelpText = "Optimize the cloning process from a repository on a local filesystem by copying files.")]
        public bool NoHardLinks { get; set; }

        [ValueList(typeof(List<string>), MaximumElements = 1)]
        public IList<string> Url { get; set; }
    }

    class OptionsWithVerbs
    {
        public OptionsWithVerbs()
        {
            CommitVerb = new CommitSubOptions();
        }

        [VerbOption("add", HelpText = "Add file contents to the index.")]
        public AddSubOptions AddVerb { get; set; }

        [VerbOption("commit", HelpText = "Record changes to the repository.")]
        public CommitSubOptions CommitVerb { get; set; }

        [VerbOption("clone", HelpText = "Clone a repository into a new directory.")]
        public CloneSubOptions CloneVerb { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            // verb is null when parser asks to print the help index (with verbs summary),
            // or parsing of no particular verb fails
            if (verb == null)
            {
                return "verbs help index";
            }
            return "help for: " + verb;
        }
    }
}
#endif