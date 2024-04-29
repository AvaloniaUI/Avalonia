#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public class RefAssemblyGenerator
{
    class Resolver : DefaultAssemblyResolver, IAssemblyResolver
    {
        readonly string _dir;
        readonly Dictionary<string, AssemblyDefinition> _cache = new();

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
        var reader = typeof(RefAssemblyGenerator).Assembly.GetManifestResourceStream("avalonia.snk")!;
        var snk = new byte[reader.Length];
        reader.ReadExactly(snk, 0, snk.Length);

        var def = AssemblyDefinition.ReadAssembly(file, new ReaderParameters
        {
            ReadWrite = true,
            InMemory = true,
            ReadSymbols = true,
            SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: true),
            AssemblyResolver = new Resolver(Path.GetDirectoryName(file)!)
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
            SymbolWriterProvider = new PortablePdbWriterProvider(),
            DeterministicMvid = def.MainModule.HasSymbols
        });
    }

    static bool HasPrivateApi(IEnumerable<CustomAttribute> attrs) => attrs.Any(a =>
        a.AttributeType.FullName == "Avalonia.Metadata.PrivateApiAttribute");
    
    static void ProcessType(TypeDefinition type, MethodReference obsoleteCtor)
    {
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
            if (!m.IsPrivate && (hideMembers || HasPrivateApi(m.CustomAttributes)))
            {
                HideMethod(m);
            }
            MarkAsUnstable(m, obsoleteCtor, forceUnstable);
        }

        foreach (var p in type.Properties)
        {
            if (HasPrivateApi(p.CustomAttributes))
            {
                if (p.SetMethod is { IsPrivate: false } setMethod)
                    HideMethod(setMethod);
                if (p.GetMethod is { IsPrivate: false } getMethod)
                    HideMethod(getMethod);
            }
        }

        foreach (var f in type.Fields)
        {
            if (!f.IsPrivate && (hideMembers || HasPrivateApi(f.CustomAttributes)))
            {
                f.IsAssembly = true;
            }
        }

        foreach (var cl in type.NestedTypes)
        {
            ProcessType(cl, obsoleteCtor);
            if (hideMembers && cl.IsNestedPublic)
            {
                cl.IsNestedAssembly = true;
            }
        }

        foreach (var m in type.Properties)
            MarkAsUnstable(m, obsoleteCtor, forceUnstable);
        foreach (var m in type.Events)
            MarkAsUnstable(m, obsoleteCtor, forceUnstable);
    }

    static void HideMethod(MethodDefinition m)
    {
        m.IsAssembly = true;
    }
    
    static void MarkAsUnstable(IMemberDefinition def, MethodReference obsoleteCtor, ICustomAttribute? unstableAttribute)
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
    
    public static void GenerateRefAsmsInPackage(string mainPackagePath, string symbolsPackagePath)
    {
        using var mainArchive = OpenPackage(mainPackagePath);
        using var symbolsArchive = OpenPackage(symbolsPackagePath);

        foreach (var entry in mainArchive.Entries
            .Where(e => e.FullName.StartsWith("ref/", StringComparison.Ordinal))
            .ToArray())
        {
            entry.Delete();
        }

        foreach (var libEntry in GetLibEntries(mainArchive, ".xml"))
        {
            var refEntry = mainArchive.CreateEntry("ref/" + libEntry.FullName.Substring(4), CompressionLevel.Optimal);
            using var src = libEntry.Open();
            using var dst = refEntry.Open();
            src.CopyTo(dst);
        }

        var pdbEntries = GetLibEntries(symbolsArchive, ".pdb").ToDictionary(e => e.FullName);

        var libs = GetLibEntries(mainArchive, ".dll")
            .Select(e => (NameParts: e.FullName.Split('/'), Entry: e))
            .Select(e => (
                Tfm: e.NameParts[1],
                DllName: e.NameParts[2],
                DllEntry: e.Entry,
                PdbName: Path.ChangeExtension(e.NameParts[2], ".pdb"),
                PdbEntry: pdbEntries.TryGetValue(Path.ChangeExtension(e.Entry.FullName, ".pdb"), out var pdbEntry) ?
                    pdbEntry :
                    throw new InvalidOperationException($"Missing symbols for {e.Entry.FullName}")))
            .GroupBy(e => e.Tfm);

        foreach (var tfm in libs)
        {
            using var _ = Helpers.UseTempDir(out var temp);

            foreach (var lib in tfm)
            {
                var extractedDllPath = Path.Combine(temp, lib.DllName);
                var extractedPdbPath = Path.Combine(temp, lib.PdbName);

                lib.DllEntry.ExtractToFile(extractedDllPath);
                lib.PdbEntry.ExtractToFile(extractedPdbPath);

                PatchRefAssembly(extractedDllPath);

                mainArchive.CreateEntryFromFile(extractedDllPath, $"ref/{lib.Tfm}/{lib.DllName}");
                symbolsArchive.CreateEntryFromFile(extractedPdbPath, $"ref/{lib.Tfm}/{lib.PdbName}");
            }
        }

        static ZipArchive OpenPackage(string packagePath)
            => new(File.Open(packagePath, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update);

        static ZipArchiveEntry[] GetLibEntries(ZipArchive archive, string extension)
            => archive.Entries
                .Where(e => e.FullName.StartsWith("lib/", StringComparison.Ordinal)
                    && e.FullName.EndsWith(extension, StringComparison.Ordinal))
                .ToArray();
    }
}
