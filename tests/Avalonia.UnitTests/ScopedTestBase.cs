using System;

namespace Avalonia.UnitTests;

public class ScopedTestBase : IDisposable
{
    private readonly IDisposable _scope;

    public ScopedTestBase()
    {
        _scope = AvaloniaLocator.EnterScope();
    }
    
    public virtual void Dispose()
    {
        _scope.Dispose();
    }
}