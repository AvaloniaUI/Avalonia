using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public static class Extensions
    {
        public static void LogError(this IBuildEngine engine, string file, string message)
        {
            engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", "0000", file ?? "", 0, 0, 0, 0, message, "",
                "Avalonia"));
        }
        
        public static void LogWarning(this IBuildEngine engine, string file, string message)
        {
            engine.LogWarningEvent(new BuildWarningEventArgs("Avalonia", "0000", file ?? "", 0, 0, 0, 0, message, "",
                "Avalonia"));
        }
    }
}
