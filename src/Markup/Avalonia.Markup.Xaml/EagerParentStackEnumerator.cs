using System.Collections.Generic;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace Avalonia.Markup.Xaml;

internal struct EagerParentStackEnumerator
{
    private IAvaloniaXamlIlEagerParentStackProvider? _provider;
    private IReadOnlyList<object>? _currentParentsStack;
    private int _currentIndex; // only valid when _currentParents isn't null

    public EagerParentStackEnumerator(IAvaloniaXamlIlEagerParentStackProvider? provider)
        => _provider = provider;

    public object? TryGetNext()
    {
        while (_provider is not null)
        {
            if (_currentParentsStack is null)
            {
                _currentParentsStack = _provider.DirectParentsStack;
                _currentIndex = _currentParentsStack.Count;
            }

            --_currentIndex;

            if (_currentIndex >= 0)
                return _currentParentsStack[_currentIndex];

            _currentParentsStack = null;
            _provider = _provider.ParentProvider;
        }

        return null;
    }

    public T? TryGetNextOfType<T>() where T : class
    {
        while (TryGetNext() is { } parent)
        {
            if (parent is T typedParent)
                return typedParent;
        }

        return null;
    }
}
