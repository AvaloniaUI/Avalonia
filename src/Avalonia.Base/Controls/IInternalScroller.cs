using System.Runtime.CompilerServices;

namespace Avalonia.Controls.Primitives;

// TODO12: Integrate with existing IScrollable interface, breaking change
internal interface IInternalScroller
{
    bool CanHorizontallyScroll { get; }

    bool CanVerticallyScroll { get; }
}
