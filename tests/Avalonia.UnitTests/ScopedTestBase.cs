using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.UnitTests;

[Collection("Scoped Not Parallel")]
public class ScopedTestBase : IDisposable
{
    private readonly IDisposable _scope;

    public ScopedTestBase()
    {
        AvaloniaLocator.Current = AvaloniaLocator.CurrentMutable = new AvaloniaLocator();
        Dispatcher.ResetBeforeUnitTests();
        Control.ResetLoadedQueueForUnitTests();
        _scope = AvaloniaLocator.EnterScope();
    }
    
    public virtual void Dispose()
    {
        Dispatcher.ResetForUnitTests();
        _scope.Dispose();
    }
}