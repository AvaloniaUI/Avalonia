using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class InputElementGestureTests : ScopedTestBase
{
    [Fact]
    public void SwipeGestureEnded_PublicEvent_CanBeObserved()
    {
        var target = new Border();
        SwipeGestureEndedEventArgs? received = null;

        target.SwipeGestureEnded += (_, e) => received = e;

        var args = new SwipeGestureEndedEventArgs(42, new Vector(12, 34));
        target.RaiseEvent(args);

        Assert.Same(args, received);
        Assert.Equal(InputElement.SwipeGestureEndedEvent, args.RoutedEvent);
    }
}
