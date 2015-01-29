// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using System.Linq;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class MatcherContext
    {
        private readonly DirectoryInfoBase _root;
        private readonly IList<PatternContextBase> _includePatternContexts;
        private readonly IList<PatternContextBase> _excludePatternContexts;
        private readonly IList<string> _files;

        private readonly IList<LiteralPathSegment> _declaredLiteralFolderSegments = new List<LiteralPathSegment>();
        private readonly IList<LiteralPathSegment> _declaredLiteralFileSegments = new List<LiteralPathSegment>();
        private bool _declaredParentPathSegment;
        private bool _declaredWildcardPathSegment;

        public MatcherContext(Matcher matcher, DirectoryInfoBase directoryInfo)
        {
            _root = directoryInfo;
            _files = new List<string>();

            _includePatternContexts = GetIncludePatternContexts(matcher.IncludePatterns);
            _excludePatternContexts = GetExcludePatternContexts(matcher.ExcludePatterns);
        }

        public PatternMatchingResult Execute()
        {
            _files.Clear();

            Match(_root, parentRelativePath: null);

            return new PatternMatchingResult(_files);
        }

        private void Match(DirectoryInfoBase directory, string parentRelativePath)
        {
            // Request all the including and excluding patterns to push current directory onto their status stack.
            PushFrame(directory);

            DeclareClear();
            foreach (var include in _includePatternContexts)
            {
                include.Declare();
            }
            foreach (var exclude in _excludePatternContexts)
            {
                exclude.Declare();
            }

            IEnumerable<FileSystemInfoBase> entities = null;
            if (_declaredWildcardPathSegment || _declaredLiteralFileSegments.Any())
            {
                entities = directory.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
            }
            else
            {
                entities = _declaredLiteralFolderSegments.Select(literal => directory.GetDirectory(literal.Value));
            }
            if (_declaredParentPathSegment)
            {
                entities = entities.Concat(new[] { directory.GetDirectory("..") });
            }

            // collect files and sub directories
            var subDirectories = new List<DirectoryInfoBase>();
            foreach (var entity in entities)
            {
                var fileInfo = entity as FileInfoBase;
                if (fileInfo != null)
                {
                    if (MatchPatternContexts(fileInfo, (pattern, file) => pattern.Test(file)))
                    {
                        _files.Add(CombinePath(parentRelativePath, fileInfo.Name));
                    }

                    continue;
                }

                var directoryInfo = entity as DirectoryInfoBase;
                if (directoryInfo != null)
                {
                    if (MatchPatternContexts(directoryInfo, (pattern, dir) => pattern.Test(dir)))
                    {
                        subDirectories.Add(directoryInfo);
                    }

                    continue;
                }
            }

            // Matches the sub directories recursively
            foreach (var subDir in subDirectories)
            {
                var relativePath = CombinePath(parentRelativePath, subDir.Name);

                Match(subDir, relativePath);
            }

            // Request all the including and excluding patterns to pop their status stack.
            PopFrame();
        }

        public void DeclareClear()
        {
            _declaredLiteralFileSegments.Clear();
            _declaredLiteralFolderSegments.Clear();
            _declaredParentPathSegment = false;
            _declaredWildcardPathSegment = false;
        }

        public void DeclareInclude(PatternSegment patternSegment, bool isLastSegment)
        {
            var literalSegment = patternSegment as LiteralPathSegment;
            if (literalSegment != null)
            {
                if (isLastSegment)
                {
                    _declaredLiteralFileSegments.Add(literalSegment);
                }
                else
                {
                    _declaredLiteralFolderSegments.Add(literalSegment);
                }
            }
            else if (patternSegment is ParentPathSegment)
            {
                _declaredParentPathSegment = true;
            }
            else if (patternSegment is WildcardPathSegment)
            {
                _declaredWildcardPathSegment = true;
            }
        }

        private string CombinePath(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }
            else
            {
                return string.Format("{0}/{1}", left, right);
            }
        }

        private bool MatchPatternContexts<TFileInfoBase>(TFileInfoBase fileinfo, Func<PatternContextBase, TFileInfoBase, bool> test)
        {
            var found = false;

            // If the given file/directory matches any including pattern, continues to next step.
            foreach (var context in _includePatternContexts)
            {
                if (test(context, fileinfo))
                {
                    found = true;
                    break;
                }
            }

            // If the given file/directory doesn't match any of the including pattern, returns false.
            if (!found)
            {
                return false;
            }

            // If the given file/directory matches any excluding pattern, returns false.
            foreach (var context in _excludePatternContexts)
            {
                if (test(context, fileinfo))
                {
                    return false;
                }
            }

            return true;
        }

        private void PopFrame()
        {
            foreach (var context in _excludePatternContexts)
            {
                context.PopFrame();
            }

            foreach (var context in _includePatternContexts)
            {
                context.PopFrame();
            }
        }

        private void PushFrame(DirectoryInfoBase directory)
        {
            foreach (var context in _includePatternContexts)
            {
                context.PushFrame(directory);
            }

            foreach (var context in _excludePatternContexts)
            {
                context.PushFrame(directory);
            }
        }

        private List<PatternContextBase> GetIncludePatternContexts(IEnumerable<Pattern> sourcePatterns)
        {
            var result = new List<PatternContextBase>();

            foreach (var pattern in sourcePatterns)
            {
                if (pattern.Contains == null)
                {
                    result.Add(new PatternContextLinearInclude(this, pattern));
                }
                else
                {
                    result.Add(new PatternContextRaggedInclude(this, pattern));
                }
            }

            return result;
        }

        private List<PatternContextBase> GetExcludePatternContexts(IEnumerable<Pattern> sourcePatterns)
        {
            var result = new List<PatternContextBase>();

            foreach (var pattern in sourcePatterns)
            {
                if (pattern.Contains == null)
                {
                    result.Add(new PatternContextLinearExclude(this, pattern));
                }
                else
                {
                    result.Add(new PatternContextRaggedExclude(this, pattern));
                }
            }

            return result;
        }
    }
}
