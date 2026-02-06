using System.Runtime.CompilerServices;
using Avalonia.Platform;

namespace Avalonia.UnitTests;

internal class MockScreen : Screen
{
    public MockScreen(double scaling, PixelRect bounds, PixelRect workingArea, bool isPrimary)
    {
        Scaling = scaling;
        Bounds = bounds;
        WorkingArea = workingArea;
        IsPrimary = isPrimary;
    }

    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(this);

    public override bool Equals(Screen? other)
        => ReferenceEquals(this, other);
}
