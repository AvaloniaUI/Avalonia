using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime
{
    public interface IAvaloniaXamlIlParentStackProvider
    {
        IEnumerable<object> Parents { get; }
    }
}
