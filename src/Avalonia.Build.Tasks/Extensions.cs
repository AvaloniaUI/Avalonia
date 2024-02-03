using System;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using XamlX;

namespace Avalonia.Build.Tasks;

internal static class Extensions
{
    public static void LogError(this IBuildEngine engine, string code, string file, Exception ex,
        int? lineNumber = null, int? linePosition = null)
    {
        if (lineNumber is null && linePosition is null
                               && ex is XmlException xe)
        {
            lineNumber = xe.LineNumber;
            linePosition = xe.LinePosition;
        }
            
#if DEBUG
        LogError(engine, code, file, ex.ToString(), lineNumber, linePosition);
#else
        LogError(engine, code, file, ex.Message, lineNumber, linePosition);
#endif
    }

    public static void LogDiagnostic(this IBuildEngine engine, XamlDiagnostic diagnostic)
    {
        var message = diagnostic.Title;

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
        int? lineNumber = null, int? linePosition = null)
    {
        engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", code, file ?? "",
            lineNumber ?? 0, linePosition ?? 0, lineNumber ?? 0, linePosition ?? 0,
            message, "", "Avalonia"));
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

    public static string FormatException(this Exception exception, bool verbose)
    {
        if (!verbose)
        {
            return exception.Message;
        }
        
        var builder = new StringBuilder();
        Process(exception);
        return builder.ToString();
         
        // Inspired by https://github.com/dotnet/msbuild/blob/e6409007d3a09255431eb28af01835ce1cd316b5/src/Shared/TaskLoggingHelper.cs#L909   
        void Process(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                foreach (Exception innerException in aggregateException.Flatten().InnerExceptions)
                {
                    Process(innerException);
                }

                return;
            }

            do
            {
                builder.Append(exception.GetType().Name);
                builder.Append(": ");
                builder.AppendLine(exception.Message);
                builder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            } while (exception != null);
        }
    }
}
