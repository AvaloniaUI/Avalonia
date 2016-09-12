// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reflection;
using System.Resources;
using System.Threading;
using Avalonia.Platform;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform : IRuntimePlatform
    {
#if NOT_NETSTANDARD
        public Assembly[] GetLoadedAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
#else
        private List<Assembly> _assemblies = null;
        public Assembly[] GetLoadedAssemblies()
        {
            if (_assemblies == null)
            {
                _assemblies = new List<Assembly>();
                foreach (var path in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
                {
                    try
                    {
                        AssemblyName an = AssemblyLoadContext.GetAssemblyName(path);
                        var assembly = Assembly.Load(an);
                        _assemblies.Add(assembly);
                    }
                    catch { }
                }
            }
            return _assemblies.ToArray();
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