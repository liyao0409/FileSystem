﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.FileProviders
{
    internal class PhysicalFilesWatcher
    {
        private readonly ConcurrentDictionary<string, FileChangeTrigger> _triggerCache =
            new ConcurrentDictionary<string, FileChangeTrigger>(StringComparer.OrdinalIgnoreCase);

        private readonly FileSystemWatcher _fileWatcher;

        private readonly object _lockObject = new object();

        private readonly string _root;

        internal PhysicalFilesWatcher(string root)
        {
            _root = root;
            _fileWatcher = new FileSystemWatcher(root);
            _fileWatcher.IncludeSubdirectories = true;
            _fileWatcher.Created += OnChanged;
            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Renamed += OnRenamed;
            _fileWatcher.Deleted += OnChanged;
        }

        internal IExpirationTrigger CreateFileChangeTrigger(string filter)
        {
            filter = NormalizeFilter(filter);
            var pattern = WildcardToRegexPattern(filter);

            FileChangeTrigger expirationTrigger;
            if (!_triggerCache.TryGetValue(pattern, out expirationTrigger))
            {
                expirationTrigger = _triggerCache.GetOrAdd(pattern, new FileChangeTrigger(pattern));
                lock (_lockObject)
                {
                    if (_triggerCache.Count > 0 && !_fileWatcher.EnableRaisingEvents)
                    {
                        // Perf: Turn on the file monitoring if there is something to monitor.
                        _fileWatcher.EnableRaisingEvents = true;
                    }
                }
            }

            return expirationTrigger;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            // For a file name change or a directory's name change raise a trigger.
            OnFileSystemEntryChange(e.OldFullPath);
            OnFileSystemEntryChange(e.FullPath);

            if (Directory.Exists(e.FullPath))
            {
                // If the renamed entity is a directory then raise trigger for every sub item.
                foreach (var newLocation in Directory.EnumerateFileSystemEntries(e.FullPath, "*", SearchOption.AllDirectories))
                {
                    // Calculated previous path of this moved item.
                    var oldLocation = newLocation.Replace(e.FullPath, e.OldFullPath);
                    OnFileSystemEntryChange(oldLocation);
                    OnFileSystemEntryChange(newLocation);
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            OnFileSystemEntryChange(e.FullPath);
        }

        private void OnFileSystemEntryChange(string fullPath)
        {
            var fileSystemInfo = new FileInfo(fullPath);
            if (FileSystemInfoHelper.IsHiddenFile(fileSystemInfo))
            {
                return;
            }

            var relativePath = fullPath.Replace(_root, string.Empty);
            if (_triggerCache.ContainsKey(relativePath))
            {
                ReportChangeForMatchedEntries(relativePath);
            }
            else
            {
                foreach (var trigger in _triggerCache.Values.Where(t => t.IsMatch(relativePath)))
                {
                    ReportChangeForMatchedEntries(trigger.Pattern);
                }
            }
        }

        private void ReportChangeForMatchedEntries(string pattern)
        {
            FileChangeTrigger expirationTrigger;
            if (_triggerCache.TryRemove(pattern, out expirationTrigger))
            {
                expirationTrigger.Changed();
                if (_triggerCache.Count == 0)
                {
                    lock (_lockObject)
                    {
                        if (_triggerCache.Count == 0 && _fileWatcher.EnableRaisingEvents)
                        {
                            // Perf: Turn off the file monitoring if no files to monitor.
                            _fileWatcher.EnableRaisingEvents = false;
                        }
                    }
                }
            }
        }

        private string NormalizeFilter(string filter)
        {
            // If the searchPath ends with \ or /, we treat searchPath as a directory,
            // and will include everything under it, recursively.
            if (IsDirectoryPath(filter))
            {
                filter = filter + "**" + Path.DirectorySeparatorChar + "*";
            }

            filter = Path.DirectorySeparatorChar == '/' ?
                filter.Replace('\\', Path.DirectorySeparatorChar) :
                filter.Replace('/', Path.DirectorySeparatorChar);

            return filter;
        }

        private bool IsDirectoryPath(string path)
        {
            return path != null && path.Length > 1 &&
                (path[path.Length - 1] == Path.DirectorySeparatorChar ||
                path[path.Length - 1] == Path.AltDirectorySeparatorChar);
        }

        private string WildcardToRegexPattern(string wildcard)
        {
            var regex = Regex.Escape(wildcard);

            if (Path.DirectorySeparatorChar == '/')
            {
                // regex wildcard adjustments for *nix-style file systems.
                regex = regex
                    .Replace(@"\*\*/", "(.*/)?") //For recursive wildcards /**/, include the current directory.
                    .Replace(@"\*\*", ".*") // For recursive wildcards that don't end in a slash e.g. **.txt would be treated as a .txt file at any depth
                    .Replace(@"\*\.\*", @"\*") // "*.*" is equivalent to "*"
                    .Replace(@"\*", @"[^/]*(/)?") // For non recursive searches, limit it any character that is not a directory separator
                    .Replace(@"\?", "."); // ? translates to a single any character
            }
            else
            {
                // regex wildcard adjustments for Windows-style file systems.
                regex = regex
                    .Replace("/", @"\\") // On Windows, / is treated the same as \.
                    .Replace(@"\*\*\\", @"(.*\\)?") //For recursive wildcards \**\, include the current directory.
                    .Replace(@"\*\*", ".*") // For recursive wildcards that don't end in a slash e.g. **.txt would be treated as a .txt file at any depth
                    .Replace(@"\*\.\*", @"\*") // "*.*" is equivalent to "*"
                    .Replace(@"\*", @"[^\\]*(\\)?") // For non recursive searches, limit it any character that is not a directory separator
                    .Replace(@"\?", "."); // ? translates to a single any character
            }

            return regex;
        }
    }
}