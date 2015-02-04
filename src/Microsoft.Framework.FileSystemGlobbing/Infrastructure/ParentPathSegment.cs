﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class ParentPathSegment : PatternSegment
    {
        public override bool TestMatchingSegment(string value, StringComparison comparisonType)
        {
            return string.Equals("..", value, System.StringComparison.Ordinal);
        }
    }
}