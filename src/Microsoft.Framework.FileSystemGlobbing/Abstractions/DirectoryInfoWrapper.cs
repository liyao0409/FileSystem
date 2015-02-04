// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Framework.FileSystemGlobbing.Abstractions
{
    public class DirectoryInfoWrapper : DirectoryInfoBase
    {
        private readonly DirectoryInfo _directoryInfo;
        private readonly bool _isParentPath;

        public DirectoryInfoWrapper(DirectoryInfo directoryInfo, bool isParentPath = false)
        {
            _directoryInfo = directoryInfo;
            _isParentPath = isParentPath;
        }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            if (_directoryInfo.Exists)
            {
                foreach (var fileSystemInfo in _directoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption))
                {
                    var directoryInfo = fileSystemInfo as DirectoryInfo;
                    if (directoryInfo != null)
                    {
                        yield return new DirectoryInfoWrapper(directoryInfo);
                    }
                    else
                    {
                        yield return new FileInfoWrapper((FileInfo)fileSystemInfo);
                    }
                }
            }
        }

        public override DirectoryInfoBase GetDirectory(string name)
        {
            return new DirectoryInfoWrapper(
                new DirectoryInfo(Path.Combine(_directoryInfo.FullName, name)),
                isParentPath: string.Equals(name, "..", StringComparison.Ordinal));
        }

        public override FileInfoBase GetFile(string name)
        {
            return new FileInfoWrapper(new FileInfo(Path.Combine(_directoryInfo.FullName, name)));
        }

        public override string Name
        {
            get { return _isParentPath ? ".." : _directoryInfo.Name; }
        }

        public override string FullName
        {
            get { return _directoryInfo.FullName; }
        }

        public override DirectoryInfoBase ParentDirectory
        {
            get { return new DirectoryInfoWrapper(_directoryInfo.Parent); }
        }
    }
}
