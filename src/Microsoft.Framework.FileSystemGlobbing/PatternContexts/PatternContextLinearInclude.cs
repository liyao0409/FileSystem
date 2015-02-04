// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.PatternContexts
{
    internal class PatternContextLinearInclude
        : PatternContextLinear
    {
        public PatternContextLinearInclude(ILinearPattern pattern)
            : base(pattern)
        {
        }

        public override void Predict(Action<IPathSegment, bool> onDeclare)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException("Can't declare path segment before enters any directory.");
            }

            if (Frame.IsNotApplicable)
            {
                return;
            }

            if (Frame.SegmentIndex < Pattern.Segments.Count)
            {
                onDeclare(Pattern.Segments[Frame.SegmentIndex], IsLastSegment());
            }
        }

        public override bool Test(FileInfoBase file)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return IsLastSegment() && TestMatchingSegment(file.Name);
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return !IsLastSegment() && TestMatchingSegment(directory.Name);
        }
    }
}
