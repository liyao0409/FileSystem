// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Microsoft.Framework.FileSystemGlobbing.PathSegments;

namespace Microsoft.Framework.FileSystemGlobbing.PatternContexts
{
    internal class PatternContextRaggedInclude : PatternContextRagged
    {
        public PatternContextRaggedInclude(IRaggedPattern pattern)
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

            if (IsStartsWith() && Frame.SegmentIndex < Frame.SegmentGroup.Count)
            {
                onDeclare(Frame.SegmentGroup[Frame.SegmentIndex], false);
            }
            else
            {
                onDeclare(WildcardPathSegment.MatchAll, false);
            }
        }

        public override bool Test(FileInfoBase file)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return IsEndsWith() && TestMatchingGroup(file);
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            if (IsStartsWith() && !TestMatchingSegment(directory.Name))
            {
                // deterministic not-included
                return false;
            }

            return true;
        }
    }
}
