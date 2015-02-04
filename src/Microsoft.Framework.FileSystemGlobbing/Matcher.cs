// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Microsoft.Framework.FileSystemGlobbing.Internal;
using Microsoft.Framework.FileSystemGlobbing.Internal.Patterns;

namespace Microsoft.Framework.FileSystemGlobbing
{
    public class Matcher
    {
        internal IList<IPattern> IncludePatterns { get; } = new List<IPattern>();

        internal IList<IPattern> ExcludePatterns { get; } = new List<IPattern>();

        public Matcher AddInclude(string pattern)
        {
            IncludePatterns.Add(PatternBuilder.Build(pattern));
            return this;
        }

        public Matcher AddExclude(string pattern)
        {
            ExcludePatterns.Add(PatternBuilder.Build(pattern));
            return this;
        }

        public PatternMatchingResult Execute(DirectoryInfoBase directoryInfo)
        {
            var context = new MatcherContext(this, directoryInfo);
            return context.Execute();
        }
    }
}