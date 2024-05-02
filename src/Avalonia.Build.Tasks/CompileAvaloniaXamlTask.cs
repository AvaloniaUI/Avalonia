using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class CompileAvaloniaXamlTask: ITask
    {
        public const string AvaloniaCompileOutputMetadataName = "AvaloniaCompileOutput";

        public bool Execute()
        {
            Enum.TryParse(ReportImportance, true, out MessageImportance outputImportance);

            var outputPath = AssemblyFile.GetMetadata(AvaloniaCompileOutputMetadataName);
            var refOutputPath = RefAssemblyFile?.GetMetadata(AvaloniaCompileOutputMetadataName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            if (!string.IsNullOrEmpty(refOutputPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(refOutputPath));
            }

            var msg = $"CompileAvaloniaXamlTask -> AssemblyFile:{AssemblyFile}, ProjectDirectory:{ProjectDirectory}, OutputPath:{outputPath}";
            BuildEngine.LogMessage(msg, outputImportance < MessageImportance.Low ? MessageImportance.High : outputImportance);

            var res = XamlCompilerTaskExecutor.Compile(BuildEngine,
                AssemblyFile.ItemSpec, outputPath,
                RefAssemblyFile?.ItemSpec, refOutputPath,
                References?.Select(i => i.ItemSpec).ToArray() ?? Array.Empty<string>(),
                ProjectDirectory, VerifyIl, DefaultCompileBindings, outputImportance,
                new XamlCompilerDiagnosticsFilter(AnalyzerConfigFiles),
                (SignAssembly && !DelaySign) ? AssemblyOriginatorKeyFile : null,
                SkipXamlCompilation, DebuggerLaunch, VerboseExceptions);

            if (res.Success && !res.WrittenFile)
            {
                // To simplify incremental build checks, copy the input files to the expected output locations even if the Xaml compiler didn't do anything.
                CopyAndTouch(AssemblyFile.ItemSpec, outputPath);
                CopyAndTouch(Path.ChangeExtension(AssemblyFile.ItemSpec, ".pdb"), Path.ChangeExtension(outputPath, ".pdb"), false);

                if (!string.IsNullOrEmpty(refOutputPath))
                {
                    CopyAndTouch(RefAssemblyFile.ItemSpec, refOutputPath);
                }
            }

            return res.Success;
        }

        private static void CopyAndTouch(string source, string destination, bool shouldExist = true)
        {
            if (!File.Exists(source))
            {
                if (shouldExist)
                {
                    throw new FileNotFoundException($"Could not copy file '{source}'. File does not exist.");
                }

                return;
            }

            File.Copy(source, destination, overwrite: true);
            File.SetLastWriteTimeUtc(destination, DateTime.UtcNow);
        }

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public ITaskItem AssemblyFile { get; set; }

        public ITaskItem? RefAssemblyFile { get; set; }

        public ITaskItem[]? References { get; set; }

        public bool VerifyIl { get; set; }

        public bool DefaultCompileBindings { get; set; }

        public bool SkipXamlCompilation { get; set; }

        public string AssemblyOriginatorKeyFile { get; set; }
        public bool SignAssembly { get; set; }
        public bool DelaySign { get; set; }

        public string ReportImportance { get; set; }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool DebuggerLaunch { get; set; }

        public bool VerboseExceptions { get; set; }

        public ITaskItem[] AnalyzerConfigFiles { get; set; }
    }
}
