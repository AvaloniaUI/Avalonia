using System;
using System.Reactive.Disposables;
using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockRuntimePlatform : IRuntimePlatform
    {
        private Assembly[] _assemblies;

        public MockRuntimePlatform(Assembly[] assemblies = null)
        {
            _assemblies = assemblies ?? new Assembly[0];
        }

        public Assembly[] GetLoadedAssemblies() => _assemblies;
        public RuntimePlatformInfo GetRuntimeInfo() => new RuntimePlatformInfo();
        public string GetStackTrace() => string.Empty;
        public void PostThreadPoolItem(Action cb) { }
        public IDisposable StartSystemTimer(TimeSpan interval, Action tick) => Disposable.Empty;
    }
}
