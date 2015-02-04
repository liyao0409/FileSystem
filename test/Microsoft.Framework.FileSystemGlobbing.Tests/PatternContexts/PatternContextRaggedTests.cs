﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Internal;
using Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Framework.FileSystemGlobbing.Internal.PatternContexts;
using Microsoft.Framework.FileSystemGlobbing.Internal.Patterns;
using Microsoft.Framework.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternContexts
{
    public class PatternContextRaggedIncludeTests
    {
        [Fact]
        public void PredictBeforeEnterDirectoryShouldThrow()
        {
            var pattern = PatternBuilder.Build("**") as IRaggedPattern;
            var context = new PatternContextRaggedInclude(pattern);

            Assert.Throws<InvalidOperationException>(() =>
            {
                context.Predict((segment, last) =>
                {
                    Assert.False(true, "No segment should be declared.");
                });
            });
        }

        [Theory]
        [InlineData("/a/b/**/c/d", new string[] { "root" }, "a", false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a" }, "b", false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b" }, null, false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b", "whatever" }, null, false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b", "whatever", "anything" }, null, false)]
        public void PredictReturnsCorrectResult(string patternString, string[] pushDirectory, string expectSegment, bool expectWildcard)
        {
            var pattern = PatternBuilder.Build(patternString) as IRaggedPattern;
            Assert.NotNull(pattern);

            var context = new PatternContextRaggedInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Predict((segment, last) =>
            {
                if (expectSegment != null)
                {
                    var mockSegment = segment as LiteralPathSegment;

                    Assert.NotNull(mockSegment);
                    Assert.Equal(false, last);
                    Assert.Equal(expectSegment, mockSegment.Value);
                }
                else
                {
                    Assert.Equal(Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments.WildcardPathSegment.MatchAll, segment);
                }
            });
        }

        [Theory]
        [InlineData("/a/b/**/c/d", new string[] { "root", "b" })]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "c" })]
        public void PredictNotCallBackWhenEnterUnmatchDirectory(string patternString, string[] pushDirectory)
        {
            var pattern = PatternBuilder.Build(patternString) as IRaggedPattern;
            var context = new PatternContextRaggedInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Predict((segment, last) =>
            {
                Assert.False(true, "No segment should be declared.");
            });
        }
    }
}