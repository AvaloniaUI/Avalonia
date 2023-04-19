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
        
        var obsoleteAttribute = new TypeRefUser(def.ManifestModule, "System", "ObsoleteAttribute", def.ManifestModule.CorLibTypes.AssemblyRef);
        var obsoleteCtor = def.ManifestModule.Import(new MemberRefUser(def.ManifestModule, ".ctor",
            new MethodSig(CallingConvention.Default, 0, def.ManifestModule.CorLibTypes.Void, new TypeSig[]
            {
                def.ManifestModule.CorLibTypes.String
            }), obsoleteAttribute));
        
        foreach(var t in def.ManifestModule.Types)
            ProcessType(t, obsoleteCtor);
        def.Write(file, new ModuleWriterOptions(def.ManifestModule)
        {
            StrongNameKey = new StrongNameKey(snk),
        });
    }

    static void ProcessType(TypeDef type, MemberRef obsoleteCtor)
    {
        foreach (var nested in type.NestedTypes)
            ProcessType(nested, obsoleteCtor);
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
                    var dflags = MethodAttributes.Public | MethodAttributes.Family | MethodAttributes.FamORAssem |
                                 MethodAttributes.FamANDAssem | MethodAttributes.Assembly;
                    m.Attributes = ((m.Attributes | dflags) ^ dflags) | MethodAttributes.Assembly;
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

            var forceUnstable = type.CustomAttributes.Any(a =>
                a.AttributeType.FullName.EndsWith("UnstableAttribute"));
            
            foreach (var m in type.Methods)
                MarkAsUnstable(m, obsoleteCtor, forceUnstable);
            foreach (var m in type.Properties)
                MarkAsUnstable(m, obsoleteCtor, forceUnstable);
            foreach (var m in type.Events)
                MarkAsUnstable(m, obsoleteCtor, forceUnstable);
            
        }
    }

    static void MarkAsUnstable(IMemberDef def, MemberRef obsoleteCtor, bool force)
    {
        if (!force
            || def.HasCustomAttributes == false
            || !def.CustomAttributes.Any(a =>
                a.AttributeType.FullName.EndsWith("UnstableAttribute")))
            return;
        
        if (def.CustomAttributes.Any(a => a.TypeFullName.EndsWith("ObsoleteAttribute")))
            return;

        def.CustomAttributes.Add(new CustomAttribute(obsoleteCtor, new CAArgument[]
        {
            new(def.Module.CorLibTypes.String,
                "This is a part of unstable API and can be changed in minor releases. You have been warned")
        }));
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