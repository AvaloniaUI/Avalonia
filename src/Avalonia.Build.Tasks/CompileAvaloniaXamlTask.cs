using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class CompileAvaloniaXamlTask: ITask
    {
        public bool Execute()
        {
            OutputPath = OutputPath ?? AssemblyFile;
            var input = AssemblyFile;
            // Make a copy and delete the original file to prevent MSBuild from thinking that everything is OK 
            if (OriginalCopyPath != null)
            {
                File.Copy(AssemblyFile, OriginalCopyPath, true);
                input = OriginalCopyPath;
                File.Delete(AssemblyFile);
            }

            var data = XamlCompilerTaskExecutor.Compile(BuildEngine, input,
                File.ReadAllLines(ReferencesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray());
            if(data == null)
                File.Copy(input, OutputPath);
            else
                File.WriteAllBytes(OutputPath, data);

            return true;
        }
        
        
        
        [Required]
        public string AssemblyFile { get; set; }
        [Required]
        public string ReferencesFilePath { get; set; }
        [Required]
        public string OriginalCopyPath { get; set; }
        
        public string OutputPath { get; set; }
        
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }
    }
}
