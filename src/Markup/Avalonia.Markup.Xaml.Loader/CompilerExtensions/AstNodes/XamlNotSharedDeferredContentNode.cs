using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes;

class XamlNotSharedDeferredContentNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
{
    private readonly IXamlMethod? _deferredContentCustomization;
    private readonly IXamlType? _deferredContentCustomizationTypeParameter;
    private readonly IXamlType _funcType;

    public IXamlAstValueNode Value { get; set; }
    public IXamlAstTypeReference Type { get; }

    public XamlNotSharedDeferredContentNode(IXamlAstValueNode value,
        IXamlMethod? deferredContentCustomization,
        IXamlType? deferredContentCustomizationTypeParameter,
        TransformerConfiguration config) : base(value)
    {
        _deferredContentCustomization = deferredContentCustomization;
        _deferredContentCustomizationTypeParameter = deferredContentCustomizationTypeParameter;
        Value = value;

        _funcType = config.TypeSystem
            .GetType("System.Func`2")
            .MakeGenericType(config.TypeMappings.ServiceProvider, config.WellKnownTypes.Object);

        var returnType = _deferredContentCustomization?.ReturnType ?? _funcType;
        Type = new XamlAstClrTypeReference(value, returnType, false);
    }

    public override void VisitChildren(IXamlAstVisitor visitor)
    {
        Value = (IXamlAstValueNode)Value.Visit(visitor);
    }

    void CompileBuilder(ILEmitContext context, XamlClosureInfo xamlClosure)
    {
        var il = context.Emitter;
        // Initialize the context
        il
            .Ldarg_0()
            .EmitCall(xamlClosure.CreateRuntimeContextMethod)
            .Stloc(context.ContextLocal);

        context.Emit(Value, context.Emitter, context.Configuration.WellKnownTypes.Object);
        il.Ret();

        context.ExecuteAfterEmitCallbacks();
    }

    public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
    {
        var so = context.Configuration.WellKnownTypes.Object;
        var isp = context.Configuration.TypeMappings.ServiceProvider;

        if (!context.TryGetItem(out XamlClosureInfo? closureInfo))
        {
            var closureType = context.DeclaringType.DefineSubType(
                so,
                "XamlClosure_" + context.Configuration.IdentifierGenerator.GenerateIdentifierPart(),
                XamlVisibility.Private);

            closureInfo = new XamlClosureInfo(closureType, context);
            context.AddAfterEmitCallbacks(() => closureType.CreateType());
            context.SetItem(closureInfo);
        }

        var counter = ++closureInfo.BuildMethodCounter;

        var buildMethod = closureInfo.Type.DefineMethod(
            so,
            new[] { isp },
            $"Build_{counter}",
            XamlVisibility.Public,
            true,
            false);

        var subContext = new ILEmitContext(
            buildMethod.Generator,
            context.Configuration,
            context.EmitMappings,
            context.RuntimeContext,
            buildMethod.Generator.DefineLocal(context.RuntimeContext.ContextType),
            closureInfo.Type,
            context.File,
            context.Emitters);

        subContext.SetItem(closureInfo);

        CompileBuilder(subContext, closureInfo);

        var customization = _deferredContentCustomization;

        if (_deferredContentCustomizationTypeParameter is not null)
            customization = customization?.MakeGenericMethod(new[] { _deferredContentCustomizationTypeParameter });

        if (customization is not null && IsFunctionPointerLike(customization.Parameters[0]))
        {
            // &Build
            codeGen
                .Ldftn(buildMethod);
        }
        else
        {
            // new Func<IServiceProvider, object>(null, &Build);
            codeGen
                .Ldnull()
                .Ldftn(buildMethod)
                .Newobj(_funcType.Constructors.First(ct =>
                    ct.Parameters.Count == 2 &&
                    ct.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
        }

        // Allow to save values from the parent context, pass own service provider, etc, etc
        if (customization is not null)
        {
            codeGen
                .Ldloc(context.ContextLocal)
                .EmitCall(customization);
        }

        return XamlILNodeEmitResult.Type(0, Type.GetClrType());
    }

    private static bool IsFunctionPointerLike(IXamlType xamlType)
        => xamlType.IsFunctionPointer // Cecil, SRE with .NET 8
           || xamlType.FullName == "System.IntPtr"; // SRE with .NET < 8 or .NET Standard

    private sealed class XamlClosureInfo
    {
        private readonly XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> _parentContext;
        private IXamlMethod? _createRuntimeContextMethod;

        public IXamlTypeBuilder<IXamlILEmitter> Type { get; }

        public IXamlMethod CreateRuntimeContextMethod
            => _createRuntimeContextMethod ??= BuildCreateRuntimeContextMethod();

        public int BuildMethodCounter { get; set; }

        public XamlClosureInfo(
            IXamlTypeBuilder<IXamlILEmitter> type,
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> parentContext)
        {
            Type = type;
            _parentContext = parentContext;
        }

        private IXamlMethod BuildCreateRuntimeContextMethod()
        {
            var method = Type.DefineMethod(
                _parentContext.RuntimeContext.ContextType,
                new[] { _parentContext.Configuration.TypeMappings.ServiceProvider },
                "CreateContext",
                XamlVisibility.Public,
                true,
                false);

            var context = new ILEmitContext(
                method.Generator,
                _parentContext.Configuration,
                _parentContext.EmitMappings,
                _parentContext.RuntimeContext,
                method.Generator.DefineLocal(_parentContext.RuntimeContext.ContextType),
                Type,
                _parentContext.File,
                _parentContext.Emitters);

            var il = context.Emitter;

            // context = new Context(arg0, ...)
            il.Ldarg_0();
            context.RuntimeContext.Factory(il);

            if (context.Configuration.TypeMappings.RootObjectProvider is { } rootObjectProviderType)
            {
                // Attempt to get the root object from parent service provider
                var noRoot = il.DefineLabel();
                using var loc = context.GetLocalOfType(context.Configuration.WellKnownTypes.Object);
                il
                    .Stloc(context.ContextLocal)
                    // if(arg == null) goto noRoot;
                    .Ldarg_0()
                    .Brfalse(noRoot)
                    // var loc = arg.GetService(typeof(IRootObjectProvider))
                    .Ldarg_0()
                    .Ldtype(rootObjectProviderType)
                    .EmitCall(context.Configuration.TypeMappings.ServiceProvider
                        .GetMethod(m => m.Name == "GetService"))
                    .Stloc(loc.Local)
                    // if(loc == null) goto noRoot;
                    .Ldloc(loc.Local)
                    .Brfalse(noRoot)
                    // loc = ((IRootObjectProvider)loc).RootObject
                    .Ldloc(loc.Local)
                    .Castclass(rootObjectProviderType)
                    .EmitCall(rootObjectProviderType
                        .GetMethod(m => m.Name == "get_RootObject"))
                    .Stloc(loc.Local)
                    // contextLocal.RootObject = loc;
                    .Ldloc(context.ContextLocal)
                    .Ldloc(loc.Local)
                    .Castclass(context.RuntimeContext.ContextType.GenericArguments[0])
                    .Stfld(context.RuntimeContext.RootObjectField!)
                    .MarkLabel(noRoot)
                    .Ldloc(context.ContextLocal);
            }

            il.Ret();

            return method;
        }
    }
}
