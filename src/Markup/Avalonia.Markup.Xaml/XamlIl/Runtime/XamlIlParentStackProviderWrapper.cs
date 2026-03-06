using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

/// <summary>
/// Wraps a <see cref="IAvaloniaXamlIlParentStackProvider"/> into a <see cref="IAvaloniaXamlIlEagerParentStackProvider"/>,
/// for backwards compatibility.
/// </summary>
internal sealed class XamlIlParentStackProviderWrapper : IAvaloniaXamlIlEagerParentStackProvider
{
    private readonly IAvaloniaXamlIlParentStackProvider _provider;

    private IReadOnlyList<object>? _directParentsStack;

    public XamlIlParentStackProviderWrapper(IAvaloniaXamlIlParentStackProvider provider)
        => _provider = provider;

    public IEnumerable<object> Parents
        => _provider.Parents;

    public IReadOnlyList<object> DirectParentsStack
        => _directParentsStack ??= _provider.Parents.Reverse().ToArray();

    public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider
        => null;
}
