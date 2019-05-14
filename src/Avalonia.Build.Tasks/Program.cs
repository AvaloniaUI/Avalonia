using System;
using System.Collections;
using System.IO;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("input references output");
                return 1;
            }

            return new CompileAvaloniaXamlTask()
            {
                AssemblyFile = args[0],
                ReferencesFilePath = args[1],
                OutputPath = args[2],
                BuildEngine = new ConsoleBuildEngine(),
                ProjectDirectory = Directory.GetCurrentDirectory()
            }.Execute() ?
                0 :
                2;
        }

        class ConsoleBuildEngine : IBuildEngine
        {
            public void LogErrorEvent(BuildErrorEventArgs e)
            {
                Console.WriteLine($"ERROR: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
            }

            public void LogWarningEvent(BuildWarningEventArgs e)
            {
                Console.WriteLine($"WARNING: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
            }

            public void LogMessageEvent(BuildMessageEventArgs e)
            {
                Console.WriteLine($"MESSAGE: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
            }

            public void LogCustomEvent(CustomBuildEventArgs e)
            {
                Console.WriteLine($"CUSTOM: {e.Message}");
            }

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
                IDictionary targetOutputs) => throw new NotSupportedException();

            public bool ContinueOnError { get; }
            public int LineNumberOfTaskNode { get; }
            public int ColumnNumberOfTaskNode { get; }
            public string ProjectFileOfTaskNode { get; }
        }
    }
}
