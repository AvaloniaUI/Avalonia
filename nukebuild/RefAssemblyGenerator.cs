using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

public class RefAssemblyGenerator
{
    static void PatchRefAssembly(string file)
    {
        
        var reader = typeof(RefAssemblyGenerator).Assembly.GetManifestResourceStream("avalonia.snk");
        var snk = new byte[reader.Length];
        reader.Read(snk, 0, snk.Length);
        
        var def = AssemblyDef.Load(new MemoryStream(File.ReadAllBytes(file)));
        
        foreach(var t in def.ManifestModule.Types)
            ProcessType(t);
        def.Write(file, new ModuleWriterOptions(def.ManifestModule)
        {
            StrongNameKey = new StrongNameKey(snk),
        });
    }

    static void ProcessType(TypeDef type)
    {
        foreach (var nested in type.NestedTypes)
            ProcessType(nested);
        if (type.IsInterface)
        {
            var hideMethods = type.Name.EndsWith("Impl");
            var injectMethod = hideMethods
                               || type.CustomAttributes.Any(a =>
                                   a.AttributeType.FullName.EndsWith("NotClientImplementableAttribute"));

            if (hideMethods)
            {
                foreach (var m in type.Methods)
                {
                    m.Attributes |= MethodAttributes.Public | MethodAttributes.Assembly;
                    m.Attributes ^= MethodAttributes.Public;
                }
            }
            
            if(injectMethod)
            {
                type.Methods.Add(new MethodDefUser("NotClientImplementable",
                    new MethodSig(CallingConvention.Default, 0, type.Module.CorLibTypes.Void),
                    MethodAttributes.Assembly
                    | MethodAttributes.Abstract
                    | MethodAttributes.NewSlot
                    | MethodAttributes.HideBySig));
            }
        }
    }
    
    public static void GenerateRefAsmsInPackage(string packagePath)
    {
        using (var archive = new ZipArchive(File.Open(packagePath, FileMode.Open, FileAccess.ReadWrite),
            ZipArchiveMode.Update))
        {
            foreach (var entry in archive.Entries.ToList())
            {
                if (entry.FullName.StartsWith("ref/"))
                    entry.Delete();
            }
            
            foreach (var entry in archive.Entries.ToList())
            {
                if (entry.FullName.StartsWith("lib/"))
                {
                    if (entry.Name.EndsWith(".dll"))
                    {
                        using (Helpers.UseTempDir(out var temp))
                        {
                            var file = Path.Combine(temp, entry.Name);
                            entry.ExtractToFile(file);
                            PatchRefAssembly(file);
                            archive.CreateEntryFromFile(file, "ref/" + entry.FullName.Substring(4));

                        }
                    }
                    else if (entry.Name.EndsWith(".xml"))
                    {
                        var newEntry = archive.CreateEntry("ref/" + entry.FullName.Substring(4),
                            CompressionLevel.Optimal);
                        using (var src = entry.Open())
                        using (var dst = newEntry.Open())
                            src.CopyTo(dst);
                    }
                }
            }
        }
    }
}