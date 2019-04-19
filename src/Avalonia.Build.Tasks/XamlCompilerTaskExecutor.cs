using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;
using Microsoft.Build.Framework;
using Mono.Cecil;
using XamlIl.TypeSystem;
using Avalonia.Utilities;
using Mono.Cecil.Rocks;
using XamlIl.Parsers;
using XamlIl.Transform;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Avalonia.Build.Tasks
{
    
    public static class XamlCompilerTaskExecutor
    {
        static bool CheckXamlName(string n) => n.ToLowerInvariant().EndsWith(".xaml")
                                               || n.ToLowerInvariant().EndsWith(".paml");
        static Dictionary<string, byte[]> ReadAvaloniaXamlResources(AssemblyDefinition asm)
        {
            var rv = new Dictionary<string, byte[]>();
            var stream = ((EmbeddedResource)asm.MainModule.Resources.FirstOrDefault(r =>
                r.ResourceType == ResourceType.Embedded && r.Name == "!AvaloniaResources"))?.GetResourceStream();
            if (stream == null)
                return rv;
            var br = new BinaryReader(stream);            
            var index = AvaloniaResourcesIndexReaderWriter.Read(new MemoryStream(br.ReadBytes(br.ReadInt32())));
            var baseOffset = stream.Position;
            foreach (var e in index.Where(e => CheckXamlName(e.Path)))
            {
                stream.Position = e.Offset + baseOffset;
                rv[e.Path] = br.ReadBytes(e.Size);
            }
            return rv;
        }

        static Dictionary<string, byte[]> ReadEmbeddedXamlResources(AssemblyDefinition asm)
        {
            var rv = new Dictionary<string, byte[]>();
            foreach (var r in asm.MainModule.Resources.OfType<EmbeddedResource>().Where(r => CheckXamlName(r.Name)))
                rv[r.Name] = r.GetResourceData();
            return rv;
        }
        
        public static byte[] Compile(IBuildEngine engine, string input, string[] references)
        {
            var typeSystem = new CecilTypeSystem(references.Concat(new[] {input}), input);
            var asm = typeSystem.TargetAssemblyDefinition;
            var emres =  ReadEmbeddedXamlResources(asm);
            var avares = ReadAvaloniaXamlResources(asm);
            if (avares.Count == 0 && emres.Count == 0)
                // Nothing to do
                return null;
            var xamlLanguage = AvaloniaXamlIlLanguage.Configure(typeSystem);
            var compilerConfig = new XamlIlTransformerConfiguration(typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                XamlIlXmlnsMappings.Resolve(typeSystem, xamlLanguage),
                AvaloniaXamlIlLanguage.CustomValueConverter);


            var contextDef = new TypeDefinition("CompiledAvaloniaXaml", "XamlIlContext", 
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);

            var contextClass = XamlIlContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage);

            var compiler = new AvaloniaXamlIlCompiler(compilerConfig, contextClass);

            var editorBrowsableAttribute = typeSystem
                .GetTypeReference(typeSystem.FindType("System.ComponentModel.EditorBrowsableAttribute"))
                .Resolve();
            var editorBrowsableCtor =
                asm.MainModule.ImportReference(editorBrowsableAttribute.GetConstructors()
                    .First(c => c.Parameters.Count == 1));

            void CompileGroup(Dictionary<string, byte[]> resources, string name, Func<string, string> uriTransform)
            {
                var typeDef = new TypeDefinition("CompiledAvaloniaXaml", name,
                    TypeAttributes.Class, asm.MainModule.TypeSystem.Object);


                typeDef.CustomAttributes.Add(new CustomAttribute(editorBrowsableCtor)
                {
                    ConstructorArguments = {new CustomAttributeArgument(editorBrowsableCtor.Parameters[0].ParameterType, 1)}
                });
                asm.MainModule.Types.Add(typeDef);
                var builder = typeSystem.CreateTypeBuilder(typeDef);
                foreach (var res in resources)
                {
                    var xaml = Encoding.UTF8.GetString(res.Value);
                    var parsed = XDocumentXamlIlParser.Parse(xaml);
                    compiler.Transform(parsed);
                    compiler.Compile(parsed, builder, contextClass,
                        "Populate:" + res.Key, "Build:" + res.Key,
                        "NamespaceInfo:" + res.Key, uriTransform(res.Key));
                }
            }

            if (emres.Count != 0)
                CompileGroup(emres, "EmbeddedResource", name => $"resm:{name}?assembly={asm.Name}");
            if (avares.Count != 0)
                CompileGroup(avares, "AvaloniaResource", name => $"avares://{asm.Name}/{name}");
            
            var ms = new MemoryStream();
            asm.Write(ms);
            return ms.ToArray();
        }
        
    }
}
