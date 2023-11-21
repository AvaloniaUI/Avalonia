using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Platform;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;
using XamlX.IL;
using XamlX.Emit;
#if RUNTIME_XAML_CECIL
using TypeAttributes = Mono.Cecil.TypeAttributes;
using Mono.Cecil;
using XamlX.Ast;
using XamlX.IL.Cecil;
#endif

namespace Avalonia.Markup.Xaml.XamlIl
{
#if !RUNTIME_XAML_CECIL
    [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
#endif
    internal static class AvaloniaXamlIlRuntimeCompiler
    {
#if !RUNTIME_XAML_CECIL
        private static SreTypeSystem _sreTypeSystem;
        private static Type _ignoresAccessChecksFromAttribute;
        private static ModuleBuilder _sreBuilder;
        private static IXamlType _sreContextType; 
        private static XamlLanguageTypeMappings _sreMappings;
        private static XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> _sreEmitMappings;
        private static XamlXmlnsMappings _sreXmlns;
        private static AssemblyBuilder _sreAsm;
        private static bool _sreCanSave;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = XamlX.TrimmingMessages.CanBeSafelyTrimmed)]
        public static void DumpRuntimeCompilationResults()
        {
            if (_sreBuilder == null)
                return;
            var saveMethod = _sreAsm.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "Save" && m.GetParameters().Length == 1);
            if (saveMethod == null)
                return;
            try
            {
                _sreBuilder.CreateGlobalFunctions();
                saveMethod.Invoke(_sreAsm, new Object[] {"XamlIlLoader.ildump"});
            }
            catch
            {
                //Ignore
            }
        }

        [CompilerDynamicDependencies]
        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = XamlX.TrimmingMessages.GeneratedTypes)]
        static void InitializeSre()
        {
            if (_sreTypeSystem == null)
                _sreTypeSystem = new SreTypeSystem();
            if (_sreBuilder == null)
            {
                _sreCanSave = !(RuntimeInformation.FrameworkDescription.StartsWith(".NET Core"));
                var name = new AssemblyName(Guid.NewGuid().ToString("N"));
                if (_sreCanSave)
                {
                    var define = GetDefineDynamicAssembly();
                    if (define != null)
                        _sreAsm = (AssemblyBuilder)define.Invoke(AppDomain.CurrentDomain, new object[]
                        {
                            name, (AssemblyBuilderAccess)3,
                            Path.GetDirectoryName(typeof(AvaloniaXamlIlRuntimeCompiler).Assembly.GetModules()[0]
                                .FullyQualifiedName)
                        });
                    else
                        _sreCanSave = false;
                }
                
                if(_sreAsm == null)
                    _sreAsm = AssemblyBuilder.DefineDynamicAssembly(name,
                        AssemblyBuilderAccess.RunAndCollect);
                
                _sreBuilder = _sreAsm.DefineDynamicModule("XamlIlLoader.ildump");
            }

            if (_sreMappings == null)
                (_sreMappings, _sreEmitMappings) = AvaloniaXamlIlLanguage.Configure(_sreTypeSystem);
            if (_sreXmlns == null)
                _sreXmlns = XamlXmlnsMappings.Resolve(_sreTypeSystem, _sreMappings);
            if (_sreContextType == null)
                _sreContextType = XamlILContextDefinition.GenerateContextClass(
                    _sreTypeSystem.CreateTypeBuilder(
                        _sreBuilder.DefineType("XamlIlContext")), _sreTypeSystem, _sreMappings,
                        _sreEmitMappings);
            if (_ignoresAccessChecksFromAttribute == null)
                _ignoresAccessChecksFromAttribute = EmitIgnoresAccessCheckAttributeDefinition(_sreBuilder);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = XamlX.TrimmingMessages.CanBeSafelyTrimmed)]
        static MethodInfo GetDefineDynamicAssembly() => AppDomain.CurrentDomain.GetType().GetMethods()
            .FirstOrDefault(m => m.Name == "DefineDynamicAssembly"
                                 && m.GetParameters().Length == 3 &&
                                 m.GetParameters()[2].ParameterType == typeof(string));

        static Type EmitIgnoresAccessCheckAttributeDefinition(ModuleBuilder builder)
        {
            var tb = builder.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
                TypeAttributes.Class | TypeAttributes.Public, typeof(Attribute));
            var field = tb.DefineField("_name", typeof(string), FieldAttributes.Private);
            var propGet = tb.DefineMethod("get_AssemblyName", MethodAttributes.Public, typeof(string),
                Array.Empty<Type>());
            var propGetIl = propGet.GetILGenerator();
            propGetIl.Emit(OpCodes.Ldarg_0);
            propGetIl.Emit(OpCodes.Ldfld, field);
            propGetIl.Emit(OpCodes.Ret);
            var prop = tb.DefineProperty("AssemblyName", PropertyAttributes.None, typeof(string), Array.Empty<Type>());
            prop.SetGetMethod(propGet);

            
            var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                new[] { typeof(string) });
            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, field);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, typeof(Attribute)
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .First(x => x.GetParameters().Length == 0));

            ctorIl.Emit(OpCodes.Ret);

            tb.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(AttributeUsageAttribute).GetConstructor(new[] { typeof(AttributeTargets) }),
                new object[] { AttributeTargets.Assembly },
                new[] { typeof(AttributeUsageAttribute).GetProperty("AllowMultiple") },
                new object[] { true }));
            
            return tb.CreateTypeInfo();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = XamlX.TrimmingMessages.GeneratedTypes)]
        static void EmitIgnoresAccessCheckToAttribute(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            if(string.IsNullOrWhiteSpace(name))
                return;
            var key = assemblyName.GetPublicKey();
            if (key != null && key.Length != 0)
                name += ", PublicKey=" + BitConverter.ToString(key).Replace("-", "").ToUpperInvariant();
            _sreAsm.SetCustomAttribute(new CustomAttributeBuilder(
                _ignoresAccessChecksFromAttribute.GetConstructors()[0],
                new object[] { name }));
        }

        static object LoadSre(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration)
        {
            var success = false;
            try
            {
                var rv = LoadSreCore(document, configuration);
                success = true;
                return rv;
            }
            finally
            {
                if(!success && _sreCanSave)
                    DumpRuntimeCompilationResults();
            }
        }

        static IReadOnlyList<object> LoadGroupSre(IReadOnlyCollection<RuntimeXamlLoaderDocument> documents,
            RuntimeXamlLoaderConfiguration configuration)
        {
            var success = false;
            try
            {
                var rv = LoadGroupSreCore(documents, configuration);
                success = true;
                return rv;
            }
            finally
            {
                if(!success &&  _sreCanSave)
                    DumpRuntimeCompilationResults();
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = XamlX.TrimmingMessages.GeneratedTypes)]
        static IReadOnlyList<object> LoadGroupSreCore(IReadOnlyCollection<RuntimeXamlLoaderDocument> documents, RuntimeXamlLoaderConfiguration configuration)
        {
            InitializeSre();
            var localAssembly = configuration.LocalAssembly;
            if (localAssembly?.GetName() != null)
                EmitIgnoresAccessCheckToAttribute(localAssembly.GetName());
            var asm = localAssembly == null ? null : _sreTypeSystem.GetAssembly(localAssembly);
            var clrPropertyBuilder = _sreBuilder.DefineType("ClrProperties_" + Guid.NewGuid().ToString("N"));
            var indexerClosureType = _sreBuilder.DefineType("IndexerClosure_" + Guid.NewGuid().ToString("N"));
            var trampolineBuilder = _sreBuilder.DefineType("Trampolines_" + Guid.NewGuid().ToString("N"));

            var diagnostics = new List<XamlDiagnostic>();
            var diagnosticsHandler = new XamlDiagnosticsHandler()
            {
                HandleDiagnostic = (diagnostic) =>
                {
                    var runtimeDiagnostic = new RuntimeXamlDiagnostic(diagnostic.Code.ToString(),
                        (RuntimeXamlDiagnosticSeverity)diagnostic.Severity,
                        diagnostic.Title, diagnostic.LineNumber, diagnostic.LinePosition)
                    {
                        Document = diagnostic.Document
                    };
                    var newSeverity =
                        (XamlDiagnosticSeverity?)configuration.DiagnosticHandler?.Invoke(runtimeDiagnostic) ??
                        diagnostic.Severity;
                    diagnostic = diagnostic with { Severity = newSeverity };
                    diagnostics.Add(diagnostic);
                    return newSeverity;
                },
                CodeMappings = AvaloniaXamlDiagnosticCodes.XamlXDiagnosticCodeToAvalonia
            };
            
            var compiler = new AvaloniaXamlIlCompiler(new AvaloniaXamlIlCompilerConfiguration(_sreTypeSystem, asm,
                    _sreMappings, _sreXmlns, AvaloniaXamlIlLanguage.CustomValueConverter,
                    new XamlIlClrPropertyInfoEmitter(_sreTypeSystem.CreateTypeBuilder(clrPropertyBuilder)),
                    new XamlIlPropertyInfoAccessorFactoryEmitter(_sreTypeSystem.CreateTypeBuilder(indexerClosureType)),
                    new XamlIlTrampolineBuilder(_sreTypeSystem.CreateTypeBuilder(trampolineBuilder)),
                    null,
                    diagnosticsHandler),
                _sreEmitMappings,
                _sreContextType)
            {
                EnableIlVerification = true,
                DefaultCompileBindings = configuration.UseCompiledBindingsByDefault,
                IsDesignMode = configuration.DesignMode
            };

            var parsedDocuments = new List<XamlDocumentResource>();
            var originalDocuments = new List<RuntimeXamlLoaderDocument>();

            foreach (var document in documents)
            {
                string xaml;
                using (var sr = new StreamReader(document.XamlStream))
                    xaml = sr.ReadToEnd();
                
                IXamlType overrideType = null;
                if (document.RootInstance != null)
                {
                    overrideType = _sreTypeSystem.GetType(document.RootInstance.GetType());
                }

                var parsed = compiler.Parse(xaml, overrideType);
                parsed.Document = "runtimexaml:" + parsedDocuments.Count;
                compiler.Transform(parsed);

                var xamlName = GetSafeUriIdentifier(document.BaseUri)
                               ?? document.RootInstance?.GetType().Name
                               ?? ((IXamlAstValueNode)parsed.Root).Type.GetClrType().Name;
                var tb = _sreBuilder.DefineType("Builder_" + Guid.NewGuid().ToString("N") + "_" + xamlName);
                var builder = _sreTypeSystem.CreateTypeBuilder(tb);

                parsedDocuments.Add(new XamlDocumentResource(
                    parsed,
                    document.BaseUri?.ToString(),
                    null,
                    null,
                    true,
                    () => new XamlDocumentTypeBuilderProvider(
                        builder,
                        compiler.DefinePopulateMethod(builder, parsed, AvaloniaXamlIlCompiler.PopulateName, XamlVisibility.Public),
                        document.RootInstance is null ?
                            compiler.DefineBuildMethod(builder, parsed, AvaloniaXamlIlCompiler.BuildName, XamlVisibility.Public) :
                            null)));
                originalDocuments.Add(document);
            }

            compiler.TransformGroup(parsedDocuments);

            diagnostics.ThrowExceptionIfAnyError();

            var createdTypes = parsedDocuments.Select(document =>
            {
                compiler.Compile(document.XamlDocument, document.TypeBuilderProvider, document.Uri, document.FileSource);
                return _sreTypeSystem.GetType(document.TypeBuilderProvider.TypeBuilder.CreateType());
            }).ToArray();
            
            clrPropertyBuilder.CreateTypeInfo();
            indexerClosureType.CreateTypeInfo();
            trampolineBuilder.CreateTypeInfo();

            return createdTypes.Zip(originalDocuments, (l, r) => (l, r))
                .Select(t => LoadOrPopulate(t.Item1, t.Item2.RootInstance, t.Item2.ServiceProvider))
                .ToArray();
        }

        static object LoadSreCore(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration)
        {
            return LoadGroupSreCore(new[] { document }, configuration).Single();
        }
#endif

        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = XamlX.TrimmingMessages.GeneratedTypes)]
        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = XamlX.TrimmingMessages.GeneratedTypes)]
        static object LoadOrPopulate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type created, object rootInstance, IServiceProvider parentServiceProvider)
        {
            var isp = Expression.Parameter(typeof(IServiceProvider));


            var epar = Expression.Parameter(typeof(object));
            var populate = created.GetMethod(AvaloniaXamlIlCompiler.PopulateName);
            isp = Expression.Parameter(typeof(IServiceProvider));
            var populateCb = Expression.Lambda<Action<IServiceProvider, object>>(
                Expression.Call(populate, isp, Expression.Convert(epar, populate.GetParameters()[1].ParameterType)),
                isp, epar).Compile();

            var serviceProvider = XamlIlRuntimeHelpers.CreateRootServiceProviderV3(parentServiceProvider);
            
            if (rootInstance == null)
            {
                var targetType = populate.GetParameters()[1].ParameterType;
                var overrideField = targetType.GetField("!XamlIlPopulateOverride",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                if (overrideField != null)
                {
                    overrideField.SetValue(null,
                        new Action<object>(
                            target => { populateCb(serviceProvider, target); }));
                    try
                    {
                        return Activator.CreateInstance(targetType);
                    }
                    finally
                    {
                        overrideField.SetValue(null, null);
                    }
                }
                
                var createCb = Expression.Lambda<Func<IServiceProvider, object>>(
                    Expression.Convert(Expression.Call(
                        created.GetMethod(AvaloniaXamlIlCompiler.BuildName), isp), typeof(object)), isp).Compile();
                return createCb(serviceProvider);
            }
            else
            {
                populateCb(serviceProvider, rootInstance);
                return rootInstance;
            }
        }

        public static object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration)
        {
#if RUNTIME_XAML_CECIL
            string xaml;
            using (var sr = new StreamReader(document.XamlStream))
                xaml = sr.ReadToEnd();
            return LoadCecil(xaml, configuration.LocalAssembly, document.RootInstance,document.BaseUri, configuration.UseCompiledBindingsByDefault);
#else
            return LoadSre(document, configuration);
#endif
        }

        public static IReadOnlyList<object> LoadGroup(IReadOnlyCollection<RuntimeXamlLoaderDocument> documents, RuntimeXamlLoaderConfiguration configuration)
        {
#if RUNTIME_XAML_CECIL
            throw new NotImplementedException("Load group was not implemented for the Cecil backend");
#else
            return LoadGroupSre(documents, configuration);
#endif
        }

        private static string GetSafeUriIdentifier(Uri uri)
        {
            return uri?.ToString()
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("?", "_")
                .Replace("=", "_")
                .Replace(".", "_");
        }
        
#if RUNTIME_XAML_CECIL
        private static Dictionary<string, (Action<IServiceProvider, object> populate, Func<IServiceProvider, object>
                build)>
            s_CecilCache =
                new Dictionary<string, (Action<IServiceProvider, object> populate, Func<IServiceProvider, object> build)
                >();


        private static string _cecilEmitDir;
        private static CecilTypeSystem _cecilTypeSystem;
        private static XamlIlLanguageTypeMappings _cecilMappings;
        private static XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> _cecilEmitMappings;
        private static XamlIlXmlnsMappings _cecilXmlns;
        private static bool _cecilInitialized;

        [CompilerDynamicDependencies]
        static void InitializeCecil()
        {
            if(_cecilInitialized)
                return;
            var path = Assembly.GetEntryAssembly().GetModules()[0].FullyQualifiedName;
            _cecilEmitDir = Path.Combine(Path.GetDirectoryName(path), "emit");
            Directory.CreateDirectory(_cecilEmitDir);
            var refs = new[] {path}.Concat(File.ReadAllLines(path + ".refs"));
            _cecilTypeSystem = new CecilTypeSystem(refs);
            (_cecilMappings, _cecilEmitMappings) = AvaloniaXamlIlLanguage.Configure(_cecilTypeSystem);
            _cecilXmlns = XamlIlXmlnsMappings.Resolve(_cecilTypeSystem, _cecilMappings);
            _cecilInitialized = true;
        }

        private static Dictionary<string, Type> _cecilGeneratedCache = new Dictionary<string, Type>();
        static object LoadCecil(string xaml, Assembly localAssembly, object rootInstance, Uri uri, bool useCompiledBindingsByDefault)
        {
            if (uri == null)
                throw new InvalidOperationException("Please, go away");
            InitializeCecil();
                        IXamlType overrideType = null;
            if (rootInstance != null)
            {
                overrideType = _cecilTypeSystem.GetType(rootInstance.GetType().FullName);
            }
           
            var safeUri = GetSafeUriIdentifier(uri);
            if (_cecilGeneratedCache.TryGetValue(safeUri, out var cached))
                return LoadOrPopulate(cached, rootInstance);
            
            
            var asm = _cecilTypeSystem.CreateAndRegisterAssembly(safeUri, new Version(1, 0),
                ModuleKind.Dll);            
            var def = new TypeDefinition("XamlIlLoader", safeUri,
                TypeAttributes.Class | TypeAttributes.Public, asm.MainModule.TypeSystem.Object);

            var contextDef = new TypeDefinition("XamlIlLoader", safeUri + "_XamlIlContext",
                TypeAttributes.Class | TypeAttributes.Public, asm.MainModule.TypeSystem.Object);
            
            asm.MainModule.Types.Add(def);
            asm.MainModule.Types.Add(contextDef);
            
            var tb = _cecilTypeSystem.CreateTypeBuilder(def);
            
            var compiler = new AvaloniaXamlIlCompiler(new XamlIlTransformerConfiguration(_cecilTypeSystem,
                    localAssembly == null ? null : _cecilTypeSystem.FindAssembly(localAssembly.GetName().Name),
                    _cecilMappings, XamlIlXmlnsMappings.Resolve(_cecilTypeSystem, _cecilMappings),
                    AvaloniaXamlIlLanguage.CustomValueConverter),
                _cecilEmitMappings,
                _cecilTypeSystem.CreateTypeBuilder(contextDef))
                {
                    DefaultCompileBindings = useCompiledBindingsByDefault
                };
            compiler.ParseAndCompile(xaml, uri.ToString(), null, tb, overrideType);
            var asmPath = Path.Combine(_cecilEmitDir, safeUri + ".dll");
            using(var f = File.Create(asmPath))
                asm.Write(f);
            var loaded = Assembly.LoadFile(asmPath)
                .GetTypes().First(x => x.Name == safeUri);
            _cecilGeneratedCache[safeUri] = loaded;
            return LoadOrPopulate(loaded, rootInstance, null);
        }
#endif
    }
}
