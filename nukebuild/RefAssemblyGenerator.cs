using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ILRepacking;
using Mono.Cecil;
using Mono.Cecil.Cil;

public class RefAssemblyGenerator
{
    class Resolver : DefaultAssemblyResolver, IAssemblyResolver
    {
        private readonly string _dir;
        Dictionary<string, AssemblyDefinition> _cache = new();

        public Resolver(string dir)
        {
            _dir = dir;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (_cache.TryGetValue(name.Name, out var asm))
                return asm;
            var path = Path.Combine(_dir, name.Name + ".dll");
            if (File.Exists(path))
                return _cache[name.Name] = AssemblyDefinition.ReadAssembly(path, parameters);
            return base.Resolve(name, parameters);
        }
    }
    
    public static void PatchRefAssembly(string file)
    {
        var reader = typeof(RefAssemblyGenerator).Assembly.GetManifestResourceStream("avalonia.snk");
        var snk = new byte[reader.Length];
        reader.Read(snk, 0, snk.Length);

        var def = AssemblyDefinition.ReadAssembly(file, new ReaderParameters
        {
            ReadWrite = true,
            InMemory = true,
            ReadSymbols = true,
            SymbolReaderProvider = new DefaultSymbolReaderProvider(false),
            AssemblyResolver = new Resolver(Path.GetDirectoryName(file))
        });

        var obsoleteAttribute = def.MainModule.ImportReference(new TypeReference("System", "ObsoleteAttribute", def.MainModule,
            def.MainModule.TypeSystem.CoreLibrary));
        var obsoleteCtor = def.MainModule.ImportReference(new MethodReference(".ctor",
            def.MainModule.TypeSystem.Void, obsoleteAttribute)
        {
            Parameters = { new ParameterDefinition(def.MainModule.TypeSystem.String) }
        });

        foreach(var t in def.MainModule.Types)
            ProcessType(t, obsoleteCtor);
        def.Write(file, new WriterParameters()
        {
            StrongNameKeyBlob = snk,
            WriteSymbols = def.MainModule.HasSymbols,
            SymbolWriterProvider = new EmbeddedPortablePdbWriterProvider(),
            DeterministicMvid = def.MainModule.HasSymbols
        });
    }

    static bool HasPrivateApi(IEnumerable<CustomAttribute> attrs) => attrs.Any(a =>
        a.AttributeType.FullName == "Avalonia.Metadata.PrivateApiAttribute");
    
    static void ProcessType(TypeDefinition type, MethodReference obsoleteCtor)
    {
        foreach (var nested in type.NestedTypes)
            ProcessType(nested, obsoleteCtor);

        var hideMembers = (type.IsInterface && type.Name.EndsWith("Impl"))
                          || HasPrivateApi(type.CustomAttributes);

        var injectMethod = hideMembers
                           || type.CustomAttributes.Any(a =>
                               a.AttributeType.FullName == "Avalonia.Metadata.NotClientImplementableAttribute");


        
        if (injectMethod)
        {
            type.Methods.Add(new MethodDefinition(
                "(This interface or abstract class is -not- implementable by user code !)",
                MethodAttributes.Assembly
                | MethodAttributes.Abstract
                | MethodAttributes.NewSlot
                | MethodAttributes.HideBySig, type.Module.TypeSystem.Void));
        }

        var forceUnstable = type.CustomAttributes.FirstOrDefault(a =>
            a.AttributeType.FullName == "Avalonia.Metadata.UnstableAttribute");

        foreach (var m in type.Methods)
        {
            if (hideMembers || HasPrivateApi(m.CustomAttributes))
            {
                HideMethod(m);
            }
            MarkAsUnstable(m, obsoleteCtor, forceUnstable);
        }

        foreach (var p in type.Properties)
        {
            if (HasPrivateApi(p.CustomAttributes))
            {
                if (p.SetMethod != null)
                    HideMethod(p.SetMethod);
                if (p.GetMethod != null)
                    HideMethod(p.GetMethod);
            }
        }

        foreach (var f in type.Fields)
        {
            if (hideMembers || HasPrivateApi(f.CustomAttributes))
            {
                var dflags = FieldAttributes.Public | FieldAttributes.Family | FieldAttributes.FamORAssem |
                             FieldAttributes.FamANDAssem | FieldAttributes.Assembly;
                f.Attributes = ((f.Attributes | dflags) ^ dflags) | FieldAttributes.Assembly;
            }
        }

        foreach (var cl in type.NestedTypes)
        {
            ProcessType(cl, obsoleteCtor);
            if (hideMembers)
            {
                var dflags = TypeAttributes.Public;
                cl.Attributes = ((cl.Attributes | dflags) ^ dflags) | TypeAttributes.NotPublic;
            }
        }

        foreach (var m in type.Properties)
            MarkAsUnstable(m, obsoleteCtor, forceUnstable);
        foreach (var m in type.Events)
            MarkAsUnstable(m, obsoleteCtor, forceUnstable);
    }

    static void HideMethod(MethodDefinition m)
    {
        var dflags = MethodAttributes.Public | MethodAttributes.Family | MethodAttributes.FamORAssem |
                     MethodAttributes.FamANDAssem | MethodAttributes.Assembly;
        m.Attributes = ((m.Attributes | dflags) ^ dflags) | MethodAttributes.Assembly;
    }
    
    static void MarkAsUnstable(IMemberDefinition def, MethodReference obsoleteCtor, ICustomAttribute unstableAttribute)
    {
        if (def.CustomAttributes.Any(a => a.AttributeType.FullName == "System.ObsoleteAttribute"))
            return;

        unstableAttribute = def.CustomAttributes.FirstOrDefault(a =>
            a.AttributeType.FullName == "Avalonia.Metadata.UnstableAttribute") ?? unstableAttribute;

        if (unstableAttribute is null)
            return;

        var message = unstableAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
        if (string.IsNullOrEmpty(message))
        {
            message = "This is a part of unstable API and can be changed in minor releases. Consider replacing it with alternatives or reach out developers on GitHub.";
        }
        
        def.CustomAttributes.Add(new CustomAttribute(obsoleteCtor)
        {
            ConstructorArguments =
            {
                new CustomAttributeArgument(obsoleteCtor.Module.TypeSystem.String, message)
            }
        });
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
                if (entry.FullName.StartsWith("lib/") && entry.Name.EndsWith(".xml"))
                {
                    var newEntry = archive.CreateEntry("ref/" + entry.FullName.Substring(4),
                        CompressionLevel.Optimal);
                    using (var src = entry.Open())
                    using (var dst = newEntry.Open())
                        src.CopyTo(dst);
                }
            }

            var libs = archive.Entries.Where(e => e.FullName.StartsWith("lib/") && e.FullName.EndsWith(".dll"))
                .Select((e => new { s = e.FullName.Split('/'), e = e }))
                .Select(e => new { Tfm = e.s[1], Name = e.s[2], Entry = e.e })
                .GroupBy(x => x.Tfm);
            foreach(var tfm in libs)
                using (Helpers.UseTempDir(out var temp))
                {
                    foreach (var l in tfm) 
                        l.Entry.ExtractToFile(Path.Combine(temp, l.Name));
                    foreach (var l in tfm) 
                        PatchRefAssembly(Path.Combine(temp, l.Name));
                    foreach (var l in tfm)
                        archive.CreateEntryFromFile(Path.Combine(temp, l.Name), $"ref/{l.Tfm}/{l.Name}");
                }
        }
    }
}
