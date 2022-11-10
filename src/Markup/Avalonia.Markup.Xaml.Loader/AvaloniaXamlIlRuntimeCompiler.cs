using System;
using System.Collections.Generic;
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
    static class AvaloniaXamlIlRuntimeCompiler
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
                    var define = AppDomain.CurrentDomain.GetType().GetMethods()
                        .FirstOrDefault(m => m.Name == "DefineDynamicAssembly"
                                    && m.GetParameters().Length == 3 &&
                                    m.GetParameters()[2].ParameterType == typeof(string));
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
        

        static object LoadSre(string xaml, Assembly localAssembly, object rootInstance, Uri uri, bool isDesignMode)
        {
            var success = false;
            try
            {
                var rv = LoadSreCore(xaml, localAssembly, rootInstance, uri, isDesignMode);
                success = true;
                return rv;
            }
            finally
            {
                if(!success && _sreCanSave)
                    DumpRuntimeCompilationResults();
            }
        }

        
        static object LoadSreCore(string xaml, Assembly localAssembly, object rootInstance, Uri uri, bool isDesignMode)
        {

            InitializeSre();
            if (localAssembly?.GetName() != null)
                EmitIgnoresAccessCheckToAttribute(localAssembly.GetName());
            var asm = localAssembly == null ? null : _sreTypeSystem.GetAssembly(localAssembly);
            var tb = _sreBuilder.DefineType("Builder_" + Guid.NewGuid().ToString("N") + "_" + uri);
            var clrPropertyBuilder = tb.DefineNestedType("ClrProperties_" + Guid.NewGuid().ToString("N"));
            var indexerClosureType = _sreBuilder.DefineType("IndexerClosure_" + Guid.NewGuid().ToString("N"));
            var trampolineBuilder = _sreBuilder.DefineType("Trampolines_" + Guid.NewGuid().ToString("N"));

            var compiler = new AvaloniaXamlIlCompiler(new AvaloniaXamlIlCompilerConfiguration(_sreTypeSystem, asm,
                _sreMappings, _sreXmlns, AvaloniaXamlIlLanguage.CustomValueConverter,
                new XamlIlClrPropertyInfoEmitter(_sreTypeSystem.CreateTypeBuilder(clrPropertyBuilder)),
                new XamlIlPropertyInfoAccessorFactoryEmitter(_sreTypeSystem.CreateTypeBuilder(indexerClosureType)),
                new XamlIlTrampolineBuilder(_sreTypeSystem.CreateTypeBuilder(trampolineBuilder))), 
                _sreEmitMappings,
                _sreContextType) { EnableIlVerification = true };
            

            IXamlType overrideType = null;
            if (rootInstance != null)
            {
                overrideType = _sreTypeSystem.GetType(rootInstance.GetType());
            }

            compiler.IsDesignMode = isDesignMode;
            compiler.ParseAndCompile(xaml, uri?.ToString(), null, _sreTypeSystem.CreateTypeBuilder(tb), overrideType);
            var created = tb.CreateTypeInfo();
            clrPropertyBuilder.CreateTypeInfo();
            indexerClosureType.CreateTypeInfo();
            trampolineBuilder.CreateTypeInfo();

            return LoadOrPopulate(created, rootInstance);
        }
#endif
        
        static object LoadOrPopulate(Type created, object rootInstance)
        {
            var isp = Expression.Parameter(typeof(IServiceProvider));


            var epar = Expression.Parameter(typeof(object));
            var populate = created.GetMethod(AvaloniaXamlIlCompiler.PopulateName);
            isp = Expression.Parameter(typeof(IServiceProvider));
            var populateCb = Expression.Lambda<Action<IServiceProvider, object>>(
                Expression.Call(populate, isp, Expression.Convert(epar, populate.GetParameters()[1].ParameterType)),
                isp, epar).Compile();
            
            if (rootInstance == null)
            {
                var targetType = populate.GetParameters()[1].ParameterType;
                var overrideField = targetType.GetField("!XamlIlPopulateOverride",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                if (overrideField != null)
                {
                    overrideField.SetValue(null,
                        new Action<object>(
                            target => { populateCb(XamlIlRuntimeHelpers.CreateRootServiceProviderV2(), target); }));
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
                return createCb(XamlIlRuntimeHelpers.CreateRootServiceProviderV2());
            }
            else
            {
                populateCb(XamlIlRuntimeHelpers.CreateRootServiceProviderV2(), rootInstance);
                return rootInstance;
            }
        }
        
        public static object Load(Stream stream, Assembly localAssembly, object rootInstance, Uri uri,
            bool isDesignMode)
        {
            string xaml;
            using (var sr = new StreamReader(stream))
                xaml = sr.ReadToEnd();
#if RUNTIME_XAML_CECIL
            return LoadCecil(xaml, localAssembly, rootInstance, uri);
#else
            return LoadSre(xaml, localAssembly, rootInstance, uri, isDesignMode);
#endif
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
        static object LoadCecil(string xaml, Assembly localAssembly, object rootInstance, Uri uri)
        {
            if (uri == null)
                throw new InvalidOperationException("Please, go away");
            InitializeCecil();
                        IXamlType overrideType = null;
            if (rootInstance != null)
            {
                overrideType = _cecilTypeSystem.GetType(rootInstance.GetType().FullName);
            }

            
           
            var safeUri = uri.ToString()
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("?", "_")
                .Replace("=", "_")
                .Replace(".", "_");
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
                _cecilTypeSystem.CreateTypeBuilder(contextDef));
            compiler.ParseAndCompile(xaml, uri.ToString(), tb, overrideType);
            var asmPath = Path.Combine(_cecilEmitDir, safeUri + ".dll");
            using(var f = File.Create(asmPath))
                asm.Write(f);
            var loaded = Assembly.LoadFile(asmPath)
                .GetTypes().First(x => x.Name == safeUri);
            _cecilGeneratedCache[safeUri] = loaded;
            return LoadOrPopulate(loaded, rootInstance);
        }
#endif
    }
}
