using UIKit;

namespace BuildTests.iOS;

internal static class Application
{
    public static void Main(string[] args)
        => UIApplication.Main(args, null, typeof(AppDelegate));
}
