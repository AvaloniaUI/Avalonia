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
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Parsers;
using XamlIl.Transform;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Avalonia.Build.Tasks
{
    
    public static partial class XamlCompilerTaskExecutor
    {
        static bool CheckXamlName(IResource r) => r.Name.ToLowerInvariant().EndsWith(".xaml")
                                               || r.Name.ToLowerInvariant().EndsWith(".paml");
        
        public class CompileResult
        {
            public bool Success { get; set; }
            public byte[] Data { get; set; }

            public CompileResult(bool success, byte[] data = null)
            {
                Success = success;
                Data = data;
            }
        }
        
        public static CompileResult Compile(IBuildEngine engine, string input, string[] references, string projectDirectory)
        {
            var typeSystem = new CecilTypeSystem(references.Concat(new[] {input}), input);
            var asm = typeSystem.TargetAssemblyDefinition;
            var emres = new EmbeddedResources(asm);
            var avares = new AvaloniaResources(asm, projectDirectory);
            if (avares.Resources.Count(CheckXamlName) == 0 && emres.Resources.Count(CheckXamlName) == 0)
                // Nothing to do
                return new CompileResult(true);
            
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

            
            var loaderDispatcherDef = new TypeDefinition("CompiledAvaloniaXaml", "!XamlLoader",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);


            loaderDispatcherDef.CustomAttributes.Add(new CustomAttribute(editorBrowsableCtor)
            {
                ConstructorArguments = {new CustomAttributeArgument(editorBrowsableCtor.Parameters[0].ParameterType, 1)}
            });


            var loaderDispatcherMethod = new MethodDefinition("TryLoad",
                MethodAttributes.Static | MethodAttributes.Public,
                asm.MainModule.TypeSystem.Object)
            {
                Parameters = {new ParameterDefinition(asm.MainModule.TypeSystem.String)}
            };
            loaderDispatcherDef.Methods.Add(loaderDispatcherMethod);
            asm.MainModule.Types.Add(loaderDispatcherDef);

            var stringEquals = asm.MainModule.ImportReference(asm.MainModule.TypeSystem.String.Resolve().Methods.First(
                m =>
                    m.IsStatic && m.Name == "Equals" && m.Parameters.Count == 2 &&
                    m.ReturnType.FullName == "System.Boolean"
                    && m.Parameters[0].ParameterType.FullName == "System.String"
                    && m.Parameters[1].ParameterType.FullName == "System.String"));
            
            bool CompileGroup(IResourceGroup group)
            {
                var typeDef = new TypeDefinition("CompiledAvaloniaXaml", "!"+ group.Name,
                    TypeAttributes.Class, asm.MainModule.TypeSystem.Object);

                typeDef.CustomAttributes.Add(new CustomAttribute(editorBrowsableCtor)
                {
                    ConstructorArguments = {new CustomAttributeArgument(editorBrowsableCtor.Parameters[0].ParameterType, 1)}
                });
                asm.MainModule.Types.Add(typeDef);
                var builder = typeSystem.CreateTypeBuilder(typeDef);
                
                foreach (var res in group.Resources.Where(CheckXamlName))
                {
                    if(res.Name.Contains("DefaultTheme"))
                        Console.WriteLine();
                    try
                    {
                        // StreamReader is needed here to handle BOM
                        var xaml = new StreamReader(new MemoryStream(res.GetData())).ReadToEnd();
                        var parsed = XDocumentXamlIlParser.Parse(xaml);

                        var initialRoot = (XamlIlAstObjectNode)parsed.Root;
                        var classDirective = initialRoot.Children.OfType<XamlIlAstXmlDirective>()
                            .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Class");
                        IXamlIlType classType = null;
                        if (classDirective != null)
                        {
                            if (classDirective.Values.Count != 1 || !(classDirective.Values[0] is XamlIlAstTextNode tn))
                                throw new XamlIlParseException("x:Class should have a string value", classDirective);
                            classType = typeSystem.TargetAssembly.FindType(tn.Text);
                            if (classType == null)
                                throw new XamlIlParseException($"Unable to find type `{tn.Text}`", classDirective);
                            initialRoot.Type = new XamlIlAstClrTypeReference(classDirective, classType);
                        }
                        
                        
                        compiler.Transform(parsed);
                        var populateName = "Populate:" + res.Name;
                        var buildName = classType == null ? "Build:" + res.Name : null; 
                        compiler.Compile(parsed, builder, contextClass,
                            populateName, buildName,
                            "NamespaceInfo:" + res.Name, res.Uri);

                        var classTypeDefinition =
                            classType == null ? null : typeSystem.GetTypeReference(classType).Resolve();
                        
                        if (classTypeDefinition != null)
                        {
                            var compiledPopulateMethod = typeSystem.GetTypeReference(builder).Resolve()
                                .Methods.First(m => m.Name == populateName);


                            const string TrampolineName = "!XamlIlPopulateTrampoline";
                            var trampoline = new MethodDefinition(TrampolineName,
                                MethodAttributes.Static | MethodAttributes.Private, asm.MainModule.TypeSystem.Void);
                            trampoline.Parameters.Add(new ParameterDefinition(classTypeDefinition));
                            classTypeDefinition.Methods.Add(trampoline);
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, compiledPopulateMethod));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                            var foundXamlLoader = false;
                            // Find AvaloniaXamlLoader.Load(this) and replace it with !XamlIlPopulateTrampoline(this)
                            foreach (var method in classTypeDefinition.Methods
                                .Where(m => !m.Attributes.HasFlag(MethodAttributes.Static)))
                            {
                                var i = method.Body.Instructions;
                                for (var c = 1; c < i.Count; c++)
                                {
                                    if (i[c - 1].OpCode == OpCodes.Ldarg_0
                                        && i[c].OpCode == OpCodes.Call)
                                    {
                                        var op = i[c].Operand as MethodReference;
                                        
                                        // TODO: Throw an error
                                        // This usually happens when same XAML resource was added twice for some weird reason
                                        // We currently support it for dual-named default theme resource
                                        if (op != null
                                            && op.Name == TrampolineName)
                                        {
                                            foundXamlLoader = true;
                                            break;
                                        }
                                        if (op != null
                                            && op.Name == "Load"
                                            && op.Parameters.Count == 1
                                            && op.Parameters[0].ParameterType.FullName == "System.Object"
                                            && op.DeclaringType.FullName == "Avalonia.Markup.Xaml.AvaloniaXamlLoader")
                                        {
                                            i[c].Operand = trampoline;
                                            foundXamlLoader = true;
                                        }
                                    }
                                }
                            }

                            if (!foundXamlLoader)
                            {
                                var ctors = classTypeDefinition.GetConstructors()
                                    .Where(c => !c.IsStatic).ToList();
                                // We can inject xaml loader into default constructor
                                if (ctors.Count == 1 && ctors[0].Body.Instructions.Count(o=>o.OpCode != OpCodes.Nop) == 3)
                                {
                                    var i = ctors[0].Body.Instructions;
                                    var retIdx = i.IndexOf(i.Last(x => x.OpCode == OpCodes.Ret));
                                    i.Insert(retIdx, Instruction.Create(OpCodes.Call, trampoline));
                                    i.Insert(retIdx, Instruction.Create(OpCodes.Ldarg_0));
                                }
                                else
                                {
                                    throw new InvalidProgramException(
                                        $"No call to AvaloniaXamlLoader.Load(this) call found anywhere in the type {classType.FullName} and type seems to have custom constructors.");
                                }
                            }

                        }

                        if (buildName != null || classTypeDefinition != null)
                        {
                            var compiledBuildMethod = buildName == null ?
                                null :
                                typeSystem.GetTypeReference(builder).Resolve()
                                    .Methods.First(m => m.Name == buildName);
                            var parameterlessConstructor = compiledBuildMethod != null ?
                                null :
                                classTypeDefinition.GetConstructors().FirstOrDefault(c =>
                                    c.IsPublic && !c.IsStatic && !c.HasParameters);

                            if (compiledBuildMethod != null || parameterlessConstructor != null)
                            {
                                var i = loaderDispatcherMethod.Body.Instructions;
                                var nop = Instruction.Create(OpCodes.Nop);
                                i.Add(Instruction.Create(OpCodes.Ldarg_0));
                                i.Add(Instruction.Create(OpCodes.Ldstr, res.Uri));
                                i.Add(Instruction.Create(OpCodes.Call, stringEquals));
                                i.Add(Instruction.Create(OpCodes.Brfalse, nop));
                                if (parameterlessConstructor != null)
                                    i.Add(Instruction.Create(OpCodes.Newobj, parameterlessConstructor));
                                else
                                {
                                    i.Add(Instruction.Create(OpCodes.Ldnull));
                                    i.Add(Instruction.Create(OpCodes.Call, compiledBuildMethod));
                                }

                                i.Add(Instruction.Create(OpCodes.Ret));
                                i.Add(nop);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        int lineNumber = 0, linePosition = 0;
                        if (e is XamlIlParseException xe)
                        {
                            lineNumber = xe.Line;
                            linePosition = xe.Position;
                        }
                        engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", "XAMLIL", res.FilePath,
                            lineNumber, linePosition, lineNumber, linePosition,
                            e.Message, "", "Avalonia"));
                        return false;
                    }
                    res.Remove();
                }
                return true;
            }
            
            if (emres.Resources.Count(CheckXamlName) != 0)
                if (!CompileGroup(emres))
                    return new CompileResult(false);
            if (avares.Resources.Count(CheckXamlName) != 0)
            {
                if (!CompileGroup(avares))
                    return new CompileResult(false);
                avares.Save();
            }
            
            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            var ms = new MemoryStream();
            asm.Write(ms);
            return new CompileResult(true, ms.ToArray());
        }
        
    }
}
