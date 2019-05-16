using System;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    static class Extensions
    {
        static string FormatErrorCode(BuildEngineErrorCode code) => $"AVLN:{(int)code:0000}";

        public static void LogError(this IBuildEngine engine, BuildEngineErrorCode code, string file, string message)
        {
            engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", FormatErrorCode(code), file ?? "", 0, 0, 0, 0, message, 
                "", "Avalonia"));
        }
        
        public static void LogWarning(this IBuildEngine engine, BuildEngineErrorCode code, string file, string message)
        {
            engine.LogWarningEvent(new BuildWarningEventArgs("Avalonia", FormatErrorCode(code), file ?? "", 0, 0, 0, 0, message,
                "", "Avalonia"));
        }
    }
}
