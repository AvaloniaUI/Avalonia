using Xunit;

namespace Avalonia.LeakTests;

/// <summary>
/// Use on leak tests where objects are somehow kept rooted in debug mode.
/// </summary>
internal sealed class ReleaseFactAttribute : FactAttribute
{
    public ReleaseFactAttribute()
    {
#if DEBUG
        Skip = "Only runs in Release mode";
#endif
    }
}
