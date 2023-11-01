using System;
using Microsoft.Build.Framework;
using XamlX;

namespace Avalonia.Build.Tasks
{
    static class Extensions
    {
        public static void LogError(this IBuildEngine engine, string code, string file, Exception ex,
            int lineNumber = 0, int linePosition = 0)
        {
#if DEBUG
            LogError(engine, code, file, ex.ToString(), lineNumber, linePosition);
#else
            LogError(engine, code, file, ex.Message, lineNumber, linePosition);
#endif
        }

        public static void LogDiagnostic(this IBuildEngine engine, XamlDiagnostic diagnostic)
        {
            var message = diagnostic.Title;
            if (string.IsNullOrWhiteSpace(diagnostic.Description))
            {
                message += Environment.NewLine;
                message += diagnostic.Description;
            }

            if (diagnostic.Severity == XamlDiagnosticSeverity.None)
            {
                // Skip.
            }
            else if (diagnostic.Severity == XamlDiagnosticSeverity.Warning)
            {
                engine.LogWarningEvent(new BuildWarningEventArgs("Avalonia", diagnostic.Code, diagnostic.Document ?? "",
                    diagnostic.LineNumber ?? 0, diagnostic.LinePosition ?? 0, 
                    diagnostic.LineNumber ?? 0, diagnostic.LinePosition ?? 0,
                    message,
                    "", "Avalonia"));
            }
            else
            {
                engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", diagnostic.Code, diagnostic.Document ?? "",
                    diagnostic.LineNumber ?? 0, diagnostic.LinePosition ?? 0, 
                    diagnostic.LineNumber ?? 0, diagnostic.LinePosition ?? 0,
                    message,
                    "", "Avalonia"));
            }
        }
        
        public static void LogError(this IBuildEngine engine, string code, string file, string message,
            int lineNumber = 0, int linePosition = 0)
        {
            engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", code, file ?? "",
                lineNumber, linePosition, lineNumber, linePosition, message,
                "", "Avalonia"));
        }

        public static void LogWarning(this IBuildEngine engine, string code, string file, string message,
            int lineNumber = 0, int linePosition = 0)
        {
            engine.LogWarningEvent(new BuildWarningEventArgs("Avalonia", code, file ?? "",
                lineNumber, linePosition, lineNumber, linePosition, message,
                "", "Avalonia"));
        }

        public static void LogMessage(this IBuildEngine engine, string message, MessageImportance imp)
        {
            engine.LogMessageEvent(new BuildMessageEventArgs(message, "", "Avalonia", imp));
        }
    }
}
