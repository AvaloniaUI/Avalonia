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
using XamlIl.Transform;
using XamlIl.TypeSystem;
#if RUNTIME_XAML_CECIL
using TypeAttributes = Mono.Cecil.TypeAttributes;
using Mono.Cecil;
using XamlIl.Ast;
#endif
namespace Avalonia.Markup.Xaml.XamlIl
{
    static class AvaloniaXamlIlRuntimeCompiler
    {
#if !RUNTIME_XAML_CECIL
        private static SreTypeSystem _sreTypeSystem;
        private static ModuleBuilder _sreBuilder;
        private static IXamlIlType _sreContextType; 
        private static XamlIlLanguageTypeMappings _sreMappings;
        private static XamlIlXmlnsMappings _sreXmlns;
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
                _sreMappings = AvaloniaXamlIlLanguage.Configure(_sreTypeSystem);
            if (_sreXmlns == null)
                _sreXmlns = XamlIlXmlnsMappings.Resolve(_sreTypeSystem, _sreMappings);
            if (_sreContextType == null)
                _sreContextType = XamlIlContextDefinition.GenerateContextClass(
                    _sreTypeSystem.CreateTypeBuilder(
                        _sreBuilder.DefineType("XamlIlContext")), _sreTypeSystem, _sreMappings);
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
            var asm = localAssembly == null ? null : _sreTypeSystem.GetAssembly(localAssembly);
            
            var compiler = new AvaloniaXamlIlCompiler(new XamlIlTransformerConfiguration(_sreTypeSystem, asm,
                _sreMappings, _sreXmlns, AvaloniaXamlIlLanguage.CustomValueConverter),
                _sreContextType);
            var tb = _sreBuilder.DefineType("Builder_" + Guid.NewGuid().ToString("N") + "_" + uri);

            IXamlIlType overrideType = null;
            if (rootInstance != null)
            {
                overrideType = _sreTypeSystem.GetType(rootInstance.GetType());
            }

            compiler.IsDesignMode = isDesignMode;
            compiler.ParseAndCompile(xaml, uri?.ToString(), null, _sreTypeSystem.CreateTypeBuilder(tb), overrideType);
            var created = tb.CreateTypeInfo();

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
            _cecilMappings = AvaloniaXamlIlLanguage.Configure(_cecilTypeSystem);
            _cecilXmlns = XamlIlXmlnsMappings.Resolve(_cecilTypeSystem, _cecilMappings);
            _cecilInitialized = true;
        }

        private static Dictionary<string, Type> _cecilGeneratedCache = new Dictionary<string, Type>();
        static object LoadCecil(string xaml, Assembly localAssembly, object rootInstance, Uri uri)
        {
            if (uri == null)
                throw new InvalidOperationException("Please, go away");
            InitializeCecil();
                        IXamlIlType overrideType = null;
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
