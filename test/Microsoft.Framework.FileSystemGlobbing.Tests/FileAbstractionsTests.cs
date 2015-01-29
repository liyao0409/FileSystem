// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests
{
    public class FileAbstractionsTests
    {
        [Fact]
        public void TempFolderStartsInitiallyEmpty()
        {
            using (var scenario = new DisposableFileSystem())
            {
                var contents = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);

                Assert.Equal(Path.GetFileName(scenario.TempFolder), scenario.DirectoryInfo.Name);
                Assert.Equal(scenario.TempFolder, scenario.DirectoryInfo.FullName);
                Assert.Equal(0, contents.Count());
            }
        }

        [Fact]
        public void FilesAreEnumerated()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFile("alpha.txt"))
            {
                var contents = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var alphaTxt = contents.OfType<FileInfoBase>().Single();

                Assert.Equal(1, contents.Count());
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        [Fact]
        public void FoldersAreEnumerated()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFolder("beta"))
            {
                var contents1 = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);

                Assert.Equal(1, contents1.Count());
                Assert.Equal("beta", beta.Name);
                Assert.Equal(0, contents2.Count());
            }
        }

        [Fact]
        public void SubFoldersAreEnumerated()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFolder("beta")
                .CreateFile("beta\\alpha.txt"))
            {
                var contents1 = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var alphaTxt = contents2.OfType<FileInfoBase>().Single();

                Assert.Equal(1, contents1.Count());
                Assert.Equal("beta", beta.Name);
                Assert.Equal(1, contents2.Count());
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        [Fact]
        public void GetDirectoryCanTakeDotDot()
        {
            using (var scenario = new DisposableFileSystem()
                .CreateFolder("gamma")
                .CreateFolder("beta")
                .CreateFile("beta\\alpha.txt"))
            {
                var gamma = scenario.DirectoryInfo.GetDirectory("gamma");
                var dotdot = gamma.GetDirectory("..");
                var contents1 = dotdot.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var beta = dotdot.GetDirectory("beta");
                var contents2 = beta.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var alphaTxt = contents2.OfType<FileInfoBase>().Single();

                Assert.Equal("..", dotdot.Name);
                Assert.Equal(2, contents1.Count());
                Assert.Equal("beta", beta.Name);
                Assert.Equal(1, contents2.Count());
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        private class DisposableFileSystem : IDisposable
        {
            public DisposableFileSystem()
            {
                TempFolder = Path.GetTempFileName();
                File.Delete(TempFolder);
                Directory.CreateDirectory(TempFolder);
                DirectoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(TempFolder));
            }

            public string TempFolder { get; }

            public DirectoryInfoBase DirectoryInfo { get; }

            public DisposableFileSystem CreateFolder(string path)
            {
                Directory.CreateDirectory(Path.Combine(TempFolder, path));
                return this;
            }

            public DisposableFileSystem CreateFile(string path)
            {
                File.WriteAllText(Path.Combine(TempFolder, path), "temp");
                return this;
            }

            public void Dispose()
            {
                Directory.Delete(TempFolder, true);
            }
        }
    }
}