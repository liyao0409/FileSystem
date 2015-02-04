﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.FileSystemGlobbing.Tests.TestUtility
{
    internal static class PatternContextHelper
    {
        public static void PushDirectory(IPatternContext context, params string[] directoryNames)
        {
            foreach (var each in directoryNames)
            {
                var directory = new MockDirectoryInfo(null, null, string.Empty, each, null);
                context.PushDirectory(directory);
            }
        }
    }
}