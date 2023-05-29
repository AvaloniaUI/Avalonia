using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia.UnitTests;

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