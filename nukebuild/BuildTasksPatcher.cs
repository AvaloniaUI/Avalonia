using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ILRepacking;
using Mono.Cecil;

public class BuildTasksPatcher
{
    public static void PatchBuildTasksInPackage(string packagePath)
    {
        using (var archive = new ZipArchive(File.Open(packagePath, FileMode.Open, FileAccess.ReadWrite),
            ZipArchiveMode.Update))
        {

            foreach (var entry in archive.Entries.ToList())
            {
                if (entry.Name == "Avalonia.Build.Tasks.dll")
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    var temp = Path.Combine(tempDir, Guid.NewGuid() + ".dll");
                    var output = temp + ".output";
                    File.Copy(typeof(Microsoft.Build.Framework.ITask).Assembly.GetModules()[0].FullyQualifiedName,
                        Path.Combine(tempDir, "Microsoft.Build.Framework.dll"));
                    var patched = new MemoryStream();
                    try
                    {
                        entry.ExtractToFile(temp, true);
                        var repack = new ILRepacking.ILRepack(new RepackOptions()
                        {
                            Internalize = true,
                            InputAssemblies = new[]
                            {
                                temp, typeof(Mono.Cecil.AssemblyDefinition).Assembly.GetModules()[0]
                                    .FullyQualifiedName,
                                typeof(Mono.Cecil.Rocks.MethodBodyRocks).Assembly.GetModules()[0].FullyQualifiedName,
                                typeof(Mono.Cecil.Pdb.PdbReaderProvider).Assembly.GetModules()[0].FullyQualifiedName,
                                typeof(Mono.Cecil.Mdb.MdbReaderProvider).Assembly.GetModules()[0].FullyQualifiedName
                                
                            },
                            SearchDirectories = new string[0],
                            OutputFile = output
                        });
                        repack.Repack();


                        // 'hurr-durr assembly with the same name is already loaded' prevention
                        using (var asm = AssemblyDefinition.ReadAssembly(output,
                            new ReaderParameters { ReadWrite = true, InMemory = true, }))
                        {
                            asm.Name = new AssemblyNameDefinition(
                                "Avalonia.Build.Tasks."
                                + Guid.NewGuid().ToString().Replace("-", ""),
                                new Version(0, 0, 0));
                            asm.Write(patched);
                            patched.Position = 0;
                        }
                    }
                    finally
                    {
                        try
                        {
                            if(Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                        }
                        catch
                        {
                            //ignore
                        }
                    }

                    var fn = entry.FullName;
                    entry.Delete();
                    var newEntry = archive.CreateEntry(fn, CompressionLevel.Optimal);
                    using (var s = newEntry.Open())
                        patched.CopyTo(s);
                }
            }
        }
    }
}