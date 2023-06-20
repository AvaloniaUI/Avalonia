namespace Avalonia.Tizen;

public static class TizenApplicationExtensions
{
    public static AppBuilder UseTizen(this AppBuilder builder)
    {
        return builder
            .UseWindowingSubsystem(TizenPlatform.Initialize, "Tizen")
            .UseSkia();
    }
}
