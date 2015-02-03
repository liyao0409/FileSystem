// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.PatternContexts;
using Microsoft.Framework.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternContexts
{
    public class PatternContextLinearIncludeTests
    {
        [Fact]
        public void PredictBeforeEnterDirectoryShouldThrow()
        {
            var pattern = MockLinearPatternBuilder.New().Add("a").Build();
            var context = new PatternContextLinearInclude(pattern);

            Assert.Throws<InvalidOperationException>(() =>
            {
                context.Predict((segment, last) =>
                {
                    Assert.False(true, "No segment should be declared.");
                });
            });
        }

        [Theory]
        [InlineData(new string[] { "a", "b" }, new string[] { "root" }, "a", false)]
        [InlineData(new string[] { "a", "b" }, new string[] { "root", "a" }, "b", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root" }, "a", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        public void PredictReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string expectSegment, bool expectLast)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Predict((Action<IPathSegment, bool>)((segment, last) =>
            {
                var literal = segment as PatternContexts.MockNonRecursivePathSegment;

                Assert.NotNull(segment);
                Assert.Equal(expectSegment, (string)literal.Value);
                Assert.Equal(expectLast, last);
            }));
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "b" })]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "c" })]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "d" })]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "c" })]
        public void PredictNotCallBackWhenEnterUnmatchDirectory(string[] testSegments, string[] pushDirectory)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Predict((segment, last) =>
            {
                Assert.False(true, "No segment should be declared.");
            });
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "d", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "c" }, "d", false)]
        public void TestFileForIncludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string filename, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new MockFileInfo(null, null, null, filename));

            Assert.Equal(expectResult, result);
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "d", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "c" }, "d", false)]
        public void TestFileForExcludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string filename, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearExclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new MockFileInfo(null, null, null, filename));

            Assert.Equal(expectResult, result);
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root" }, "a", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "b", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "c", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", false)]
        public void TestDiretcoryForIncludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string directoryName, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new MockDirectoryInfo(null, null, null, directoryName, null));

            Assert.Equal(expectResult, result);
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root" }, "a", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "c", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        public void TestDiretcoryForExcludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string directoryName, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearExclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new MockDirectoryInfo(null, null, null, directoryName, null));

            Assert.Equal(expectResult, result);
        }
    }
}