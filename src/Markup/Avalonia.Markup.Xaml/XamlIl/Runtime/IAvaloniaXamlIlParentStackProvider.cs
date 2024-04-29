using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime
{
    public interface IAvaloniaXamlIlParentStackProvider
    {
        IEnumerable<object> Parents { get; }
    }

    public interface IAvaloniaXamlIlEagerParentStackProvider : IAvaloniaXamlIlParentStackProvider
    {
        IReadOnlyList<object> DirectParents { get; }

        IAvaloniaXamlIlEagerParentStackProvider? ParentProvider { get; }
    }
}
