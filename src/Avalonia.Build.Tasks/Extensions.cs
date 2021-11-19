using System;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    static class Extensions
    {
        static string FormatErrorCode(BuildEngineErrorCode code) => $"AVLN:{(int)code:0000}";

        public static void LogError(this IBuildEngine engine, BuildEngineErrorCode code, string file, string message, int lineNumber = 0, int columnNumber = 0, int endLineNumber = 0, int endColumnNumber = 0)
        {
            engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", FormatErrorCode(code), file ?? engine.ProjectFileOfTaskNode, lineNumber, columnNumber, endLineNumber, endColumnNumber, message,
                "", "Avalonia")
            {
                ProjectFile = engine.ProjectFileOfTaskNode,
            });
        }

        public static void LogWarning(this IBuildEngine engine, BuildEngineErrorCode code, string file, string message, int lineNumber = 0, int columnNumber = 0, int endLineNumber = 0, int endColumnNumber = 0)
        {
            engine.LogWarningEvent(new BuildWarningEventArgs("Avalonia", FormatErrorCode(code), file ?? engine.ProjectFileOfTaskNode, lineNumber, columnNumber, endLineNumber, endColumnNumber, message,
                "", "Avalonia")
            {
                ProjectFile = engine.ProjectFileOfTaskNode,
            });
        }

        public static void LogMessage(this IBuildEngine engine, string message, MessageImportance imp)
        {
            engine.LogMessageEvent(new BuildMessageEventArgs(message, "", "Avalonia", imp)
            {
                ProjectFile = engine.ProjectFileOfTaskNode,
            });
        }
    }
}
