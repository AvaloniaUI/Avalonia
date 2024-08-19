using MonoMac.AppKit;

namespace IntegrationTestApp.Embedding;

internal class MacHelper
{
    private static bool s_isInitialized;

    public static void EnsureInitialized()
    {
        if (s_isInitialized)
            return;
        s_isInitialized = true;
        NSApplication.Init();
    }
}
