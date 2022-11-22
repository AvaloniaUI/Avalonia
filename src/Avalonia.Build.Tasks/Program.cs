using System;
using System.Collections;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class Program
    {
        private const string OriginalDll = "original.dll";
        private const string References = "references";
        private const string OutDll = "out.dll";

        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                if (args.Length == 1)
                {
                    args = new[] {OriginalDll, References, OutDll}
                        .Select(x => Path.Combine(args[0], x)).ToArray();
                }
                else
                {
                    const string referencesOutputPath = "path/to/Avalonia/samples/Sandbox/obj/Debug/net60/Avalonia";
                    Console.WriteLine(@$"Usage:
    1) dotnet ./Avalonia.Build.Tasks.dll <ReferencesOutputPath>
       , where <ReferencesOutputPath> likes {referencesOutputPath}
    2) dotnet ./Avalonia.Build.Tasks.dll <AssemblyFilePath> <ReferencesFilePath> <OutputPath> <RefAssemblyFile>
       , where:
           - <AssemblyFilePath> likes {referencesOutputPath}/{OriginalDll}
           - <ReferencesFilePath> likes {referencesOutputPath}/{References}
           - <OutputPath> likes {referencesOutputPath}/{OutDll}
           - <RefAssemblyFile> Likes {referencesOutputPath}/original.ref.dll");

                    return 1;
                }
            }

            return new CompileAvaloniaXamlTask()
            {
                AssemblyFile = args[0],
                ReferencesFilePath = args[1],
                OutputPath = args[2],
                RefAssemblyFile = args.Length > 3 ? args[3] : null, 
                BuildEngine = new ConsoleBuildEngine(),
                ProjectDirectory = Directory.GetCurrentDirectory(),
                VerifyIl = true
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
