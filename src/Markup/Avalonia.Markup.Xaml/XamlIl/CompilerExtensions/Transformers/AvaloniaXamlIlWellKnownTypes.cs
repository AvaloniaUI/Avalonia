using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlWellKnownTypes
    {
        public IXamlType AvaloniaObject { get; }
        public IXamlType IAvaloniaObject { get; }
        public IXamlType BindingPriority { get; }
        public IXamlType AvaloniaObjectExtensions { get; }
        public IXamlType AvaloniaProperty { get; }
        public IXamlType AvaloniaPropertyT { get; }
        public IXamlType IBinding { get; }
        public IXamlMethod AvaloniaObjectBindMethod { get; }
        public IXamlMethod AvaloniaObjectSetValueMethod { get; }
        public IXamlType IDisposable { get; }
        public XamlTypeWellKnownTypes XamlIlTypes { get; }
        public XamlLanguageTypeMappings XamlIlMappings { get; }
        public IXamlType Transitions { get; }
        public IXamlType AssignBindingAttribute { get; }
        public IXamlType UnsetValueType { get; }
        public IXamlType StyledElement { get; }
        public IXamlType NameScope { get; }
        public IXamlMethod NameScopeSetNameScope { get; }
        public IXamlType INameScope { get; }
        public IXamlMethod INameScopeRegister { get; }
        public IXamlMethod INameScopeComplete { get; }
        
        public AvaloniaXamlIlWellKnownTypes(TransformerConfiguration config)
        {
            XamlIlTypes = config.WellKnownTypes;
            XamlIlMappings = config.TypeMappings;
            AvaloniaObject = config.TypeSystem.GetType("Avalonia.AvaloniaObject");
            IAvaloniaObject = config.TypeSystem.GetType("Avalonia.IAvaloniaObject");
            AvaloniaObjectExtensions = config.TypeSystem.GetType("Avalonia.AvaloniaObjectExtensions");
            AvaloniaProperty = config.TypeSystem.GetType("Avalonia.AvaloniaProperty");
            AvaloniaPropertyT = config.TypeSystem.GetType("Avalonia.AvaloniaProperty`1");
            BindingPriority = config.TypeSystem.GetType("Avalonia.Data.BindingPriority");
            IBinding = config.TypeSystem.GetType("Avalonia.Data.IBinding");
            IDisposable = config.TypeSystem.GetType("System.IDisposable");
            Transitions = config.TypeSystem.GetType("Avalonia.Animation.Transitions");
            AssignBindingAttribute = config.TypeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            AvaloniaObjectBindMethod = AvaloniaObjectExtensions.FindMethod("Bind", IDisposable, false, IAvaloniaObject,
                AvaloniaProperty,
                IBinding, config.WellKnownTypes.Object);
            UnsetValueType = config.TypeSystem.GetType("Avalonia.UnsetValueType");
            StyledElement = config.TypeSystem.GetType("Avalonia.StyledElement");
            INameScope = config.TypeSystem.GetType("Avalonia.Controls.INameScope");
            INameScopeRegister = INameScope.GetMethod(
                new FindMethodMethodSignature("Register", XamlIlTypes.Void,
                     XamlIlTypes.String, XamlIlTypes.Object)
                {
                    IsStatic = false, DeclaringOnly = true, IsExactMatch = true
                });
            INameScopeComplete = INameScope.GetMethod(
                new FindMethodMethodSignature("Complete", XamlIlTypes.Void)
                {
                    IsStatic = false, DeclaringOnly = true, IsExactMatch = true
                });
            NameScope = config.TypeSystem.GetType("Avalonia.Controls.NameScope");
            NameScopeSetNameScope = NameScope.GetMethod(new FindMethodMethodSignature("SetNameScope",
                XamlIlTypes.Void, StyledElement, INameScope) {IsStatic = true});

            AvaloniaObjectSetValueMethod = AvaloniaObject.FindMethod("SetValue", XamlIlTypes.Void,
                false, AvaloniaProperty, XamlIlTypes.Object, BindingPriority);
            
        }
    }

    static class AvaloniaXamlIlWellKnownTypesExtensions
    {
        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this AstTransformationContext ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }

        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }
    }
}
