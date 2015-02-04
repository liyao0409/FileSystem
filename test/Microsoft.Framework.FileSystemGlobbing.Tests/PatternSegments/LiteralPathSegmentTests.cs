﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.PathSegments;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternSegments
{
    public class LiteralPathSegmentTests
    {
        [Fact]
        public void AllowNullInDefaultConstructor()
        {
            var pathSegment = new LiteralPathSegment(null);
            Assert.NotNull(pathSegment);
        }

        [Fact]
        public void AllowEmptyInDefaultConstructor()
        {
            var pathSegment = new LiteralPathSegment(string.Empty);
            Assert.NotNull(pathSegment);
        }

        [Theory]
        [InlineData("something", "anything", StringComparison.Ordinal, false)]
        [InlineData("something", "Something", StringComparison.Ordinal, false)]
        [InlineData("something", "something", StringComparison.Ordinal, true)]
        [InlineData("something", "anything", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("something", "Something", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("something", "something", StringComparison.OrdinalIgnoreCase, true)]
        public void Match(string initialValue, string testSample, StringComparison comparerType, bool expectation)
        {
            var pathSegment = new LiteralPathSegment(initialValue);
            Assert.Equal(initialValue, pathSegment.Value);
            Assert.Equal(expectation, pathSegment.Match(testSample, comparerType));
        }
    }
}