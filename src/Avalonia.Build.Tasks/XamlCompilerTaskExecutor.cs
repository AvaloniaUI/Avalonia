using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using XamlX.IL;

namespace Avalonia.Build.Tasks
{
    public static partial class XamlCompilerTaskExecutor
    {
        private const string CompiledAvaloniaXamlNamespace = "CompiledAvaloniaXaml";
        
        static bool CheckXamlName(IResource r) => r.Name.ToLowerInvariant().EndsWith(".xaml")
                                               || r.Name.ToLowerInvariant().EndsWith(".paml")
                                               || r.Name.ToLowerInvariant().EndsWith(".axaml");
        
        public class CompileResult
        {
            public bool Success { get; set; }
            public bool WrittenFile { get; }

            public CompileResult(bool success, bool writtenFile = false)
            {
                Success = success;
                WrittenFile = writtenFile;
            }
        }

        public static CompileResult Compile(IBuildEngine engine,
            string input, string output,
            string refInput, string refOutput,
            string[] references, string projectDirectory,
            bool verifyIl, bool defaultCompileBindings, MessageImportance logImportance, string strongNameKey,
            bool skipXamlCompilation)
        {
            return Compile(engine, input, output, refInput, refOutput, references, projectDirectory, verifyIl, defaultCompileBindings, logImportance, strongNameKey, skipXamlCompilation, debuggerLaunch:false);
        }

        internal static CompileResult Compile(IBuildEngine engine,
            string input, string output,
            string refInput, string refOutput,
            string[] references, string projectDirectory,
            bool verifyIl, bool defaultCompileBindings, MessageImportance logImportance, string strongNameKey, bool skipXamlCompilation, bool debuggerLaunch)
        {
            try
            {
                references = references.Where(r => !r.ToLowerInvariant().EndsWith("avalonia.build.tasks.dll")).ToArray();
                var typeSystem = new CecilTypeSystem(references, input);
                var refTypeSystem = !string.IsNullOrWhiteSpace(refInput) && File.Exists(refInput) ? new CecilTypeSystem(references, refInput) : null;

                var asm = typeSystem.TargetAssemblyDefinition;
                var refAsm = refTypeSystem?.TargetAssemblyDefinition;
                if (!skipXamlCompilation)
                {
	                var compileRes = CompileCore(engine, typeSystem, projectDirectory, verifyIl, defaultCompileBindings, logImportance, debuggerLaunch);
	                if (compileRes == null)
	                    return new CompileResult(true);
	                if (compileRes == false)
	                    return new CompileResult(false);

	                if (refTypeSystem is not null)
	                {
	                    var refCompileRes = CompileCoreForRefAssembly(engine, typeSystem, refTypeSystem);
	                    if (refCompileRes == false)
	                        return new CompileResult(false);
	                }
                }

                var writerParameters = new WriterParameters { WriteSymbols = asm.MainModule.HasSymbols };
                if (!string.IsNullOrWhiteSpace(strongNameKey))
	                writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKey);

                asm.Write(output, writerParameters);

                var refWriterParameters = new WriterParameters { WriteSymbols = false };
                if (!string.IsNullOrWhiteSpace(strongNameKey))
	                writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKey);
                refAsm?.Write(refOutput, refWriterParameters);

                return new CompileResult(true, true);
            }
            catch (Exception ex)
            {
                engine.LogError(BuildEngineErrorCode.Unknown, "", ex.Message);
                return new CompileResult(false);
            }
        }

        static bool? CompileCore(IBuildEngine engine, CecilTypeSystem typeSystem,
            string projectDirectory, bool verifyIl,
            bool defaultCompileBindings,
            MessageImportance logImportance
            , bool debuggerLaunch = false)
        {
            if (debuggerLaunch)
            {
                // According this https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.debugger.launch?view=net-6.0#remarks
                // documentation, on not windows platform Debugger.Launch() always return true without running a debugger.
                if (System.Diagnostics.Debugger.Launch())
                {
                    // Set timeout at 1 minut.
                    var time = new System.Diagnostics.Stopwatch();
                    var timeout = TimeSpan.FromMinutes(1);
                    time.Start();

                    // wait for the debugger to be attacked or timeout.
                    while (!System.Diagnostics.Debugger.IsAttached && time.Elapsed < timeout)
                    {
                        engine.LogMessage($"[PID:{System.Diagnostics.Process.GetCurrentProcess().Id}] Wating attach debugger. Elapsed {time.Elapsed}...", MessageImportance.High);
                        System.Threading.Thread.Sleep(100);
                    }

                    time.Stop();                    
                    if (time.Elapsed >= timeout)
                    {
                        engine.LogMessage("Wating attach debugger timeout.", MessageImportance.Normal);
                    }
                }
                else
                {
                    engine.LogMessage("Debugging cancelled.", MessageImportance.Normal);
                }
            }
            var asm = typeSystem.TargetAssemblyDefinition;
            var avares = new AvaloniaResources(asm, projectDirectory);
            if (avares.Resources.Count(CheckXamlName) == 0)
                // Nothing to do
                return null;

            if (typeSystem.FindType("System.Reflection.AssemblyMetadataAttribute") is {} asmMetadata)
            {
                var ctor = asm.MainModule.ImportReference(typeSystem.GetTypeReference(asmMetadata).Resolve()
                    .GetConstructors().First(c => c.Parameters.Count == 2).Resolve());
                var strType = asm.MainModule.ImportReference(typeof(string));
                var arg1 = new CustomAttributeArgument(strType, "AvaloniaUseCompiledBindingsByDefault");
                var arg2 = new CustomAttributeArgument(strType, defaultCompileBindings.ToString());
                asm.CustomAttributes.Add(new CustomAttribute(ctor) { ConstructorArguments = { arg1, arg2 } });
            }

            var clrPropertiesDef = new TypeDefinition(CompiledAvaloniaXamlNamespace, "XamlIlHelpers",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(clrPropertiesDef);
            var indexerAccessorClosure = new TypeDefinition(CompiledAvaloniaXamlNamespace, "!IndexerAccessorFactoryClosure",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(indexerAccessorClosure);
            var trampolineBuilder = new TypeDefinition(CompiledAvaloniaXamlNamespace, "XamlIlTrampolines",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(trampolineBuilder);

            var (xamlLanguage , emitConfig) = AvaloniaXamlIlLanguage.Configure(typeSystem);
            var compilerConfig = new AvaloniaXamlIlCompilerConfiguration(typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                XamlXmlnsMappings.Resolve(typeSystem, xamlLanguage),
                AvaloniaXamlIlLanguage.CustomValueConverter,
                new XamlIlClrPropertyInfoEmitter(typeSystem.CreateTypeBuilder(clrPropertiesDef)),
                new XamlIlPropertyInfoAccessorFactoryEmitter(typeSystem.CreateTypeBuilder(indexerAccessorClosure)),
                new XamlIlTrampolineBuilder(typeSystem.CreateTypeBuilder(trampolineBuilder)),
                new DeterministicIdGenerator());


            var contextDef = new TypeDefinition(CompiledAvaloniaXamlNamespace, "XamlIlContext", 
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);

            var contextClass = XamlILContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler = new AvaloniaXamlIlCompiler(compilerConfig, emitConfig, contextClass) { EnableIlVerification = verifyIl, DefaultCompileBindings = defaultCompileBindings };

            var editorBrowsableAttribute = typeSystem
                .GetTypeReference(typeSystem.FindType("System.ComponentModel.EditorBrowsableAttribute"))
                .Resolve();
            var editorBrowsableCtor =
                asm.MainModule.ImportReference(editorBrowsableAttribute.GetConstructors()
                    .First(c => c.Parameters.Count == 1));

            var runtimeHelpers = typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");
            var createRootServiceProviderMethod = asm.MainModule.ImportReference(
                typeSystem.GetTypeReference(runtimeHelpers).Resolve().Methods
                    .First(x => x.Name == "CreateRootServiceProviderV2"));
            
            var loaderDispatcherDef = new TypeDefinition(CompiledAvaloniaXamlNamespace, "!XamlLoader",
                TypeAttributes.Class | TypeAttributes.Public, asm.MainModule.TypeSystem.Object);


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
                    m.IsStatic && m.Name == "Equals" && m.Parameters.Count == 3 &&
                    m.ReturnType.FullName == "System.Boolean"
                    && m.Parameters[0].ParameterType.FullName == "System.String"
                    && m.Parameters[1].ParameterType.FullName == "System.String"
                    && m.Parameters[2].ParameterType.FullName == "System.StringComparison"));
            
            bool CompileGroup(IResourceGroup group)
            {
                var typeDef = new TypeDefinition(CompiledAvaloniaXamlNamespace, "!"+ group.Name,
                    TypeAttributes.Class | TypeAttributes.Public, asm.MainModule.TypeSystem.Object);

                typeDef.CustomAttributes.Add(new CustomAttribute(editorBrowsableCtor)
                {
                    ConstructorArguments = {new CustomAttributeArgument(editorBrowsableCtor.Parameters[0].ParameterType, 1)}
                });
                asm.MainModule.Types.Add(typeDef);
                var builder = typeSystem.CreateTypeBuilder(typeDef);

                var populateMethodsToTransform = new List<(MethodDefinition populateMethod, string resourceFilePath)>();
                
                IReadOnlyCollection<XamlDocumentResource> parsedXamlDocuments = new List<XamlDocumentResource>();
                foreach (var res in group.Resources.Where(CheckXamlName).OrderBy(x => x.FilePath.ToLowerInvariant()))
                {
                    engine.LogMessage($"XAMLIL: {res.Name} -> {res.Uri}", logImportance);

                    try
                    {
                        // StreamReader is needed here to handle BOM
                        var xaml = new StreamReader(new MemoryStream(res.FileContents)).ReadToEnd();
                        var parsed = XDocumentXamlParser.Parse(xaml);

                        var initialRoot = (XamlAstObjectNode)parsed.Root;
                        
                        
                        var precompileDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                            .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Precompile");
                        if (precompileDirective != null)
                        {
                            var precompileText = (precompileDirective.Values[0] as XamlAstTextNode)?.Text.Trim()
                                .ToLowerInvariant();
                            if (precompileText == "false")
                                continue;
                            if (precompileText != "true")
                                throw new XamlParseException("Invalid value for x:Precompile", precompileDirective);
                        }
                        
                        var classDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                            .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Class");
                        IXamlType classType = null;
                        if (classDirective != null)
                        {
                            if (classDirective.Values.Count != 1 || !(classDirective.Values[0] is XamlAstTextNode tn))
                                throw new XamlParseException("x:Class should have a string value", classDirective);
                            classType = typeSystem.TargetAssembly.FindType(tn.Text);
                            if (classType == null)
                                throw new XamlParseException($"Unable to find type `{tn.Text}`", classDirective);
                            compiler.OverrideRootType(parsed,
                                new XamlAstClrTypeReference(classDirective, classType, false));
                            initialRoot.Children.Remove(classDirective);
                        }
                        
                        
                        compiler.Transform(parsed);

                        var populateName = classType == null ? "Populate:" + res.Name : "!XamlIlPopulate";
                        var buildName = classType == null ? "Build:" + res.Name : null;
                        var classTypeDefinition =
                            classType == null ? null : typeSystem.GetTypeReference(classType).Resolve();


                        var populateBuilder = classTypeDefinition == null ?
                            builder :
                            typeSystem.CreateTypeBuilder(classTypeDefinition);

                        ((List<XamlDocumentResource>)parsedXamlDocuments).Add(new XamlDocumentResource(
                            parsed, res.Uri, res, classType,
                            populateBuilder,
                            compiler.DefinePopulateMethod(populateBuilder, parsed, populateName,
                                classTypeDefinition == null),
                            buildName == null ? null : compiler.DefineBuildMethod(builder, parsed, buildName, true)));
                    }
                    catch (Exception e)
                    {
                        int lineNumber = 0, linePosition = 0;
                        if (e is XamlParseException xe)
                        {
                            lineNumber = xe.LineNumber;
                            linePosition = xe.LinePosition;
                        }

                        engine.LogError(BuildEngineErrorCode.TransformError, res.FilePath, e.Message, lineNumber, linePosition);
                        return false;
                    }
                }

                try
                {
                    compiler.TransformGroup(parsedXamlDocuments);
                }
                catch (XamlDocumentParseException e)
                {
                    engine.LogError(BuildEngineErrorCode.TransformError, e.FilePath, e.Message, e.LineNumber, e.LinePosition);
                }
                catch (XamlParseException e)
                {
                    engine.LogError(BuildEngineErrorCode.TransformError, "", e.Message, e.LineNumber, e.LinePosition);
                }

                foreach (var document in parsedXamlDocuments)
                {
                    var parsed = document.XamlDocument;
                    var res = (IResource)document.FileSource;
                    var classType = document.ClassType;
                    var populateBuilder = document.TypeBuilder;

                    try
                    {
                        var classTypeDefinition =
                            classType == null ? null : typeSystem.GetTypeReference(classType).Resolve();

                        compiler.Compile(parsed, 
                            contextClass,
                            document.PopulateMethod,
                            document.BuildMethod,
                            builder.DefineSubType(compilerConfig.WellKnownTypes.Object, "NamespaceInfo:" + res.Name,
                                true),
                            (closureName, closureBaseType) =>
                                populateBuilder.DefineSubType(closureBaseType, closureName, false),
                            (closureName, returnType, parameterTypes) =>
                                populateBuilder.DefineDelegateSubType(closureName, false, returnType, parameterTypes),
                            res.Uri, res
                        );

                        var compiledPopulateMethod = typeSystem.GetTypeReference(populateBuilder).Resolve()
                            .Methods.First(m => m.Name == populateName);

                        // Include populate method and all nested methods/closures used in the populate method,
                        // So we can replace style/resource includes in all of them. 
                        populateMethodsToTransform.Add((compiledPopulateMethod, res.FilePath));
                        populateMethodsToTransform.AddRange(compiledPopulateMethod.Body.Instructions
                            .Where(b => b.OpCode == OpCodes.Call || b.OpCode == OpCodes.Callvirt || b.OpCode == OpCodes.Ldftn)
                            .Select(b => b.Operand)
                            .OfType<MethodReference>()
                            .Where(m => compiledPopulateMethod.DeclaringType.NestedTypes.Contains(m.DeclaringType))
                            .Select(m => m.Resolve())
                            .Where(m => m.HasBody)
                            .Select(m => (m, res.FilePath)));

                        if (classTypeDefinition != null)
                        {
                            var designLoaderFieldType = typeSystem
                                .GetType("System.Action`1")
                                .MakeGenericType(typeSystem.GetType("System.Object"));

                            var designLoaderFieldTypeReference = (GenericInstanceType)typeSystem.GetTypeReference(designLoaderFieldType);
                            designLoaderFieldTypeReference.GenericArguments[0] =
                                asm.MainModule.ImportReference(designLoaderFieldTypeReference.GenericArguments[0]);
                            designLoaderFieldTypeReference = (GenericInstanceType)
                                asm.MainModule.ImportReference(designLoaderFieldTypeReference);
                            
                            var designLoaderLoad =
                                typeSystem.GetMethodReference(
                                    designLoaderFieldType.Methods.First(m => m.Name == "Invoke"));
                            designLoaderLoad =
                                asm.MainModule.ImportReference(designLoaderLoad);
                            designLoaderLoad.DeclaringType = designLoaderFieldTypeReference;

                            var designLoaderField = new FieldDefinition("!XamlIlPopulateOverride",
                                FieldAttributes.Static | FieldAttributes.Private, designLoaderFieldTypeReference);
                            classTypeDefinition.Fields.Add(designLoaderField);

                            const string TrampolineName = "!XamlIlPopulateTrampoline";
                            var trampoline = new MethodDefinition(TrampolineName,
                                MethodAttributes.Static | MethodAttributes.Private, asm.MainModule.TypeSystem.Void);
                            trampoline.Parameters.Add(new ParameterDefinition(classTypeDefinition));
                            classTypeDefinition.Methods.Add(trampoline);

                            var regularStart = Instruction.Create(OpCodes.Call, createRootServiceProviderMethod);
                            
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, designLoaderField));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, regularStart));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, designLoaderField));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, designLoaderLoad));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                            
                            trampoline.Body.Instructions.Add(regularStart);
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, compiledPopulateMethod));
                            trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                            CopyDebugDocument(trampoline, compiledPopulateMethod);

                            var foundXamlLoader = false;
                            // Find AvaloniaXamlLoader.Load(this) and replace it with !XamlIlPopulateTrampoline(this)
                            foreach (var method in classTypeDefinition.Methods
                                .Where(m => !m.Attributes.HasFlag(MethodAttributes.Static)))
                            {
                                var i = method.Body.Instructions;
                                for (var c = 1; c < i.Count; c++)
                                {
                                    if (i[c].OpCode == OpCodes.Call)
                                    {
                                        var op = i[c].Operand as MethodReference;
                                        
                                        // TODO: Throw an error
                                        // This usually happens when the same XAML resource was added twice for some weird reason
                                        // We currently support it for dual-named default theme resources
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
                                            if (MatchThisCall(i, c - 1))
                                            {
                                                i[c].Operand = trampoline;
                                                foundXamlLoader = true;
                                            }
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

                        if (document.BuildMethod != null || classTypeDefinition != null)
                        {
                            var compiledBuildMethod = document.BuildMethod == null ?
                                null :
                                typeSystem.GetTypeReference(builder).Resolve()
                                    .Methods.First(m => m.Name == document.BuildMethod.Name);
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
                                i.Add(Instruction.Create(OpCodes.Ldc_I4, (int)StringComparison.OrdinalIgnoreCase));
                                i.Add(Instruction.Create(OpCodes.Call, stringEquals));
                                i.Add(Instruction.Create(OpCodes.Brfalse, nop));
                                if (parameterlessConstructor != null)
                                    i.Add(Instruction.Create(OpCodes.Newobj, parameterlessConstructor));
                                else
                                {
                                    i.Add(Instruction.Create(OpCodes.Call, createRootServiceProviderMethod));
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
                        if (e is XamlParseException xe)
                        {
                            lineNumber = xe.LineNumber;
                            linePosition = xe.LinePosition;
                        }
                        engine.LogError(BuildEngineErrorCode.EmitError, res.FilePath, e.Message, lineNumber, linePosition);
                        return false;
                    }
                    res.Remove();
                }

                foreach (var (populateMethod, resourceFilePath) in populateMethodsToTransform)
                {
                    if (!TransformXamlIncludes(engine, typeSystem, populateMethod, resourceFilePath, createRootServiceProviderMethod))
                    {
                        return false;
                    }
                }

                // Technically that's a hack, but it fixes corert incompatibility caused by deterministic builds
                int dupeCounter = 1;
                foreach (var grp in typeDef.NestedTypes.GroupBy(x => x.Name))
                {
                    if (grp.Count() > 1)
                    {
                        foreach (var dupe in grp)
                            dupe.Name += "_dup" + dupeCounter++;
                    }
                }
                
                
                return true;
            }
            
            if (avares.Resources.Count(CheckXamlName) != 0)
            {
                if (!CompileGroup(avares))
                    return false;
                avares.Save();
            }
            
            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return true;
        }

        static bool? CompileCoreForRefAssembly(
            IBuildEngine engine, CecilTypeSystem sourceTypeSystem, CecilTypeSystem refTypeSystem)
        {
            var asm = refTypeSystem.TargetAssemblyDefinition;

            var compiledTypes = sourceTypeSystem.TargetAssemblyDefinition.MainModule.Types
                .Where(t => t.Namespace.StartsWith(CompiledAvaloniaXamlNamespace) && t.IsPublic).ToArray();
            if (compiledTypes.Length == 0)
            {
                return null;
            }

            try
            {
                foreach (var ogType in compiledTypes)
                {
                    var wrappedOgType = sourceTypeSystem.TargetAssembly.FindType(ogType.FullName);
                    
                    var clrPropertiesDef = new TypeDefinition(ogType.Namespace, ogType.Name,
                        TypeAttributes.Class | TypeAttributes.Public, asm.MainModule.TypeSystem.Object);
                    asm.MainModule.Types.Add(clrPropertiesDef);
                    foreach (var attribute in ogType.CustomAttributes)
                    {
                        var method = asm.MainModule.ImportReference(attribute.Constructor);
                        clrPropertiesDef.CustomAttributes.Add(new CustomAttribute(method, attribute.GetBlob()));
                    }

                    var typeBuilder = refTypeSystem.CreateTypeBuilder(clrPropertiesDef);
                    foreach (var ogMethod in wrappedOgType.Methods.Where(m => m.IsPublic && m.IsStatic))
                    {
                        var method = typeBuilder.DefineMethod(ogMethod.ReturnType, ogMethod.Parameters, ogMethod.Name,
                            ogMethod.IsPublic, ogMethod.IsStatic, false);
                        method.Generator.Ldnull();
                        method.Generator.Throw();
                    }

                    typeBuilder.CreateType();
                }
            }
            catch (Exception e)
            {
                engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", "XAMLIL", "",
                    0, 0, 0, 0,
                    e.Message, "", "Avalonia"));
                return false;
            }

            return true;
        }
        
        private static bool TransformXamlIncludes(
            IBuildEngine engine, CecilTypeSystem typeSystem,
            MethodDefinition populateMethod, string resourceFilePath,
            MethodReference createRootServiceProviderMethod)
        {
            var asm = typeSystem.TargetAssemblyDefinition;
            foreach (var instruction in populateMethod.Body.Instructions.ToArray())
            {
                const string resolveStyleIncludeName = "ResolveStyleInclude";
                const string resolveResourceInclude = "ResolveResourceInclude";
                if (instruction.OpCode == OpCodes.Call
                    && instruction.Operand is MethodReference
                    {
                        Name: resolveStyleIncludeName or resolveResourceInclude,
                        DeclaringType: { Name: "XamlIlRuntimeHelpers" }
                    })
                {
                    int lineNumber = 0, linePosition = 0;
                    bool instructionsModified = false;
                    try
                    {
                        var assetSource = (string)instruction.Previous.Previous.Previous.Operand;
                        lineNumber = GetConstValue(instruction.Previous.Previous);
                        linePosition = GetConstValue(instruction.Previous);

                        var index = populateMethod.Body.Instructions.IndexOf(instruction);

                        assetSource = assetSource.Replace("avares://", "");

                        var assemblyNameSeparator = assetSource.IndexOf('/');
                        var fileNameSeparator = assetSource.LastIndexOf('/');
                        if (assemblyNameSeparator < 0 || fileNameSeparator < 0)
                        {
                            throw new InvalidProgramException(
                                $"Invalid asset source \"{assetSource}\". It must contain assembly name and relative path.");
                        }

                        var assetAssemblyName = assetSource.Substring(0, assemblyNameSeparator);
                        var assetAssembly = typeSystem.FindAssembly(assetAssemblyName)
                                            ?? throw new InvalidProgramException(
                                                $"Unable to resolve assembly \"{assetAssemblyName}\"");

                        var fileName = Path.GetFileNameWithoutExtension(assetSource.Replace('/', '.'));
                        if (assetAssembly.FindType(fileName) is { } type
                            && type.FindConstructor() is { } ctor)
                        {
                            var ctorMethod =
                                asm.MainModule.ImportReference(typeSystem.GetMethodReference(ctor));
                            instructionsModified = true;
                            populateMethod.Body.Instructions[index - 3] = Instruction.Create(OpCodes.Nop);
                            populateMethod.Body.Instructions[index - 2] = Instruction.Create(OpCodes.Nop);
                            populateMethod.Body.Instructions[index - 1] = Instruction.Create(OpCodes.Nop);
                            populateMethod.Body.Instructions[index] = Instruction.Create(OpCodes.Newobj, ctorMethod);
                        }
                        else
                        {
                            var resources = assetAssembly.FindType("CompiledAvaloniaXaml.!AvaloniaResources")
                                            ?? throw new InvalidOperationException(
                                                $"Unable to resolve \"!AvaloniaResources\" type on \"{assetAssemblyName}\" assembly");

                            var relativeName = "Build:" + assetSource.Substring(assemblyNameSeparator);
                            var buildMethod = resources.FindMethod(m => m.Name == relativeName)
                                              ?? throw new InvalidOperationException(
                                                  $"Unable to resolve build method \"{relativeName}\" resource on the \"{assetAssemblyName}\" assembly");

                            var methodReference = asm.MainModule.ImportReference(typeSystem.GetMethodReference(buildMethod));
                            instructionsModified = true;
                            populateMethod.Body.Instructions[index - 3] = Instruction.Create(OpCodes.Nop);
                            populateMethod.Body.Instructions[index - 2] = Instruction.Create(OpCodes.Nop);
                            populateMethod.Body.Instructions[index - 1] =
                                Instruction.Create(OpCodes.Call, createRootServiceProviderMethod);
                            populateMethod.Body.Instructions[index] = Instruction.Create(OpCodes.Call, methodReference);
                        }
                    }
                    catch (Exception e)
                    {
                        if (instructionsModified)
                        {
                            engine.LogErrorEvent(new BuildErrorEventArgs("Avalonia", "XAMLIL", resourceFilePath,
                                lineNumber, linePosition, lineNumber, linePosition,
                                e.Message, "", "Avalonia"));
                            return false;
                        }
                        else
                        {
                            engine.LogWarningEvent(new BuildWarningEventArgs("Avalonia", "XAMLIL",
                                resourceFilePath,
                                lineNumber, linePosition, lineNumber, linePosition,
                                e.Message, "", "Avalonia"));
                        }
                    }
                    
                    static int GetConstValue(Instruction instruction)
                    {
                        if (instruction.OpCode is { Code : >= Code.Ldc_I4_0 and <= Code.Ldc_I4_8 })
                        {
                            return instruction.OpCode.Code - Code.Ldc_I4_0;
                        }
                        if (instruction.Operand is not null)
                        {
                            return Convert.ToInt32(instruction.Operand);
                        }

                        return 0;
                    }
                }
            }

            return true;
        }
    }
}
