using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;
using Avalonia.Platform;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl
{
    public static class AvaloniaXamlIlRuntimeCompiler
    {
        private static SreTypeSystem _typeSystem;
        private static ModuleBuilder _builder;
        private static XamlIlLanguageTypeMappings _mappings;
        private static XamlIlXmlnsMappings _xmlns;
        private static AssemblyBuilder _asm;
        private static bool _canSave;

        public static void DumpRuntimeCompilationResults()
        {
            var saveMethod = _asm.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "Save" && m.GetParameters().Length == 1);
            if (saveMethod == null)
                throw new PlatformNotSupportedException();
            try
            {
                _builder.CreateGlobalFunctions();
                saveMethod.Invoke(_asm, new Object[] {"XamlIlLoader.ildump"});
            }
            catch
            {
                //Ignore
            }
        }
        
        static void Initialize()
        {
            if (_typeSystem == null)
                _typeSystem = new SreTypeSystem();
            if (_builder == null)
            {
                _canSave = !AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetRuntimeInfo().IsCoreClr;
                var name = new AssemblyName(Guid.NewGuid().ToString("N"));
                if (_canSave)
                {
                    var define = AppDomain.CurrentDomain.GetType().GetMethods()
                        .FirstOrDefault(m => m.Name == "DefineDynamicAssembly"
                                    && m.GetParameters().Length == 3 &&
                                    m.GetParameters()[2].ParameterType == typeof(string));
                    if (define != null)
                        _asm = (AssemblyBuilder)define.Invoke(AppDomain.CurrentDomain, new object[]
                        {
                            name, (AssemblyBuilderAccess)3,
                            Path.GetDirectoryName(typeof(AvaloniaXamlIlRuntimeCompiler).Assembly.GetModules()[0]
                                .FullyQualifiedName)
                        });
                    else
                        _canSave = false;
                }
                
                if(_asm == null)
                    _asm = AssemblyBuilder.DefineDynamicAssembly(name,
                        AssemblyBuilderAccess.RunAndCollect);
                
                _builder = _asm.DefineDynamicModule("XamlIlLoader.ildump");
                
            }

            if (_mappings == null)
                _mappings = AvaloniaXamlIlLanguage.Configure(_typeSystem);
            if (_xmlns == null)
                _xmlns = XamlIlXmlnsMappings.Resolve(_typeSystem, _mappings);
        }

        public static object Load(Stream stream, Assembly localAssembly, object rootInstance, Uri uri)
        {
            var success = false;
            try
            {
                var rv = LoadCore(stream, localAssembly, rootInstance, uri);
                success = true;
                return rv;
            }
            finally
            {
                if(!success && _canSave)
                    DumpRuntimeCompilationResults();
            }
        }
        
        static object LoadCore(Stream stream, Assembly localAssembly, object rootInstance, Uri uri)
        {
            string xaml;
            using (var sr = new StreamReader(stream))
                xaml = sr.ReadToEnd();
            Initialize();
            var asm = localAssembly == null ? null : _typeSystem.GetAssembly(localAssembly);
            var compiler = new AvaloniaXamlIlCompiler(new XamlIlTransformerConfiguration(_typeSystem, asm,
                _mappings, _xmlns, CustomValueConverter));
            var tb = _builder.DefineType("Builder_" + Guid.NewGuid().ToString("N") + "_" + uri);

            IXamlIlType overrideType = null;
            if (rootInstance != null)
            {
                overrideType = _typeSystem.GetType(rootInstance.GetType());
            }

            compiler.ParseAndCompile(xaml, uri?.ToString(), _typeSystem.CreateTypeBuilder(tb), overrideType);
            var created = tb.CreateTypeInfo();

            var isp = Expression.Parameter(typeof(IServiceProvider));
            if (rootInstance == null)
            {
                var createCb = Expression.Lambda<Func<IServiceProvider, object>>(
                    Expression.Convert(Expression.Call(
                        created.GetMethod(AvaloniaXamlIlCompiler.BuildName), isp), typeof(object)), isp).Compile();
                return createCb(null);
            }
            else
            {
                var epar = Expression.Parameter(typeof(object));
                var populate = created.GetMethod(AvaloniaXamlIlCompiler.PopulateName);
                isp = Expression.Parameter(typeof(IServiceProvider));
                var populateCb = Expression.Lambda<Action<IServiceProvider, object>>(
                    Expression.Call(populate, isp, Expression.Convert(epar, populate.GetParameters()[1].ParameterType)),
                    isp, epar).Compile();
                populateCb(null, rootInstance);
                return rootInstance;
            }
        }

        private static bool CustomValueConverter(XamlIlAstTransformationContext context,
            IXamlIlAstValueNode node, IXamlIlType type, out IXamlIlAstValueNode result)
        {
            if (type.FullName == "System.TimeSpan" 
                && node is XamlIlAstTextNode tn
                && !tn.Text.Contains(":"))
            {
                var seconds = double.Parse(tn.Text, CultureInfo.InvariantCulture);
                result = new XamlIlStaticOrTargetedReturnMethodCallNode(tn,
                    type.FindMethod("FromSeconds", type, false, context.Configuration.WellKnownTypes.Double),
                    new[]
                    {
                        new XamlIlConstantNode(tn, context.Configuration.WellKnownTypes.Double, seconds)
                    });
                return true;
            }

            result = null;
            return false;
        }
    }
}
