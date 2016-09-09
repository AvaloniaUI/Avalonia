// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reflection;
using System.Resources;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform : IRuntimePlatform
    {
#if NOT_NETSTANDARD
        public Assembly[] GetLoadedAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
#else
        public Assembly[] GetLoadedAssemblies()
        {
            Type appDomainType = Type.GetType("AppDomain");
            var currentDomainProperty = appDomainType.GetTypeInfo().GetProperty("CurrentDomain");
            var currentDomain = currentDomainProperty.GetMethod.Invoke(null, null);
            var getAssembliesMethod = appDomainType.GetTypeInfo().GetMethod("GetAssemblies");
            return (Assembly[])getAssembliesMethod.Invoke(currentDomain, null);
        }
#endif
        public void PostThreadPoolItem(Action cb) => ThreadPool.QueueUserWorkItem(_ => cb(), null);
        public IDisposable StartSystemTimer(TimeSpan interval, Action tick)
        {
            var timer = new Timer(delegate
            {

            }, null, interval, interval);
            return Disposable.Create(() => timer.Dispose());
        }



        public string GetStackTrace() => Environment.StackTrace;
    }
}