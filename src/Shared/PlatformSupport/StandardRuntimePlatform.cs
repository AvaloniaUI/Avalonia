// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using System.Resources;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform : IRuntimePlatform
    {

#if NETSTANDARD
        public void PostThreadPoolItem(Action cb) =>  ThreadPool.QueueUserWorkItem(_ => cb(), null);
#else
        public Assembly[] GetLoadedAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
        public void PostThreadPoolItem(Action cb) => ThreadPool.UnsafeQueueUserWorkItem(_ => cb(), null);
#endif
        public IDisposable StartSystemTimer(TimeSpan interval, Action tick)
        {
            return new Timer(_ => tick(), null, interval, interval);
        }



        public string GetStackTrace() => Environment.StackTrace;
    }
}