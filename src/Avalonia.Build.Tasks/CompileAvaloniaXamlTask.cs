using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class CompileAvaloniaXamlTask: ITask
    {
        public bool Execute()
        {
            OutputPath = OutputPath ?? AssemblyFile;
            var outputPdb = GetPdbPath(OutputPath);
            var input = AssemblyFile;
            var inputPdb = GetPdbPath(input);
            // Make a copy and delete the original file to prevent MSBuild from thinking that everything is OK 
            if (OriginalCopyPath != null)
            {
                File.Copy(AssemblyFile, OriginalCopyPath, true);
                input = OriginalCopyPath;
                File.Delete(AssemblyFile);

                if (File.Exists(inputPdb))
                {
                    var copyPdb = GetPdbPath(OriginalCopyPath);
                    File.Copy(inputPdb, copyPdb, true);
                    File.Delete(inputPdb);
                    inputPdb = copyPdb;
                }
            }

            var res = XamlCompilerTaskExecutor.Compile(BuildEngine, input,
                File.ReadAllLines(ReferencesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray(),
                ProjectDirectory, OutputPath);
            if (!res.Success)
                return false;
            if (!res.WrittenFile)
            {
                File.Copy(input, OutputPath, true);
                if(File.Exists(inputPdb))
                    File.Copy(inputPdb, outputPdb, true);
            }
            return true;
        }

        string GetPdbPath(string p)
        {
            var d = Path.GetDirectoryName(p);
            var f = Path.GetFileNameWithoutExtension(p);
            var rv = f + ".pdb";
            if (d != null)
                rv = Path.Combine(d, rv);
            return rv;
        }
        
        [Required]
        public string AssemblyFile { get; set; }
        [Required]
        public string ReferencesFilePath { get; set; }
        [Required]
        public string OriginalCopyPath { get; set; }
        [Required]
        public string ProjectDirectory { get; set; }
        
        public string OutputPath { get; set; }
        
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }
    }
}
