using System.Runtime.CompilerServices;

namespace Avalonia.Controls.Primitives;

internal interface IInternalScroller
{
    bool CanHorizontallyScroll { get; }

    bool CanVerticallyScroll { get; }
}
