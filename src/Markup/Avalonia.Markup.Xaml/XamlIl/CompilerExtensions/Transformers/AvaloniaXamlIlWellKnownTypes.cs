using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlWellKnownTypes
    {
        public IXamlIlType AvaloniaObject { get; }
        public IXamlIlType IAvaloniaObject { get; }
        public IXamlIlType BindingPriority { get; }
        public IXamlIlType AvaloniaObjectExtensions { get; }
        public IXamlIlType AvaloniaProperty { get; }
        public IXamlIlType AvaloniaPropertyT { get; }
        public IXamlIlType IBinding { get; }
        public IXamlIlMethod AvaloniaObjectBindMethod { get; }
        public IXamlIlMethod AvaloniaObjectSetValueMethod { get; }
        public IXamlIlType IDisposable { get; }
        public XamlIlTypeWellKnownTypes XamlIlTypes { get; }
        public IXamlIlType Transitions { get; }
        public IXamlIlType AssignBindingAttribute { get; }
        public IXamlIlType UnsetValueType { get; }
        public IXamlIlType StyledElement { get; }
        public IXamlIlType NameScope { get; }
        public IXamlIlMethod NameScopeStaticRegister { get; }
        
        public AvaloniaXamlIlWellKnownTypes(XamlIlAstTransformationContext ctx)
        {
            XamlIlTypes = ctx.Configuration.WellKnownTypes;
            AvaloniaObject = ctx.Configuration.TypeSystem.GetType("Avalonia.AvaloniaObject");
            IAvaloniaObject = ctx.Configuration.TypeSystem.GetType("Avalonia.IAvaloniaObject");
            AvaloniaObjectExtensions = ctx.Configuration.TypeSystem.GetType("Avalonia.AvaloniaObjectExtensions");
            AvaloniaProperty = ctx.Configuration.TypeSystem.GetType("Avalonia.AvaloniaProperty");
            AvaloniaPropertyT = ctx.Configuration.TypeSystem.GetType("Avalonia.AvaloniaProperty`1");
            BindingPriority = ctx.Configuration.TypeSystem.GetType("Avalonia.Data.BindingPriority");
            IBinding = ctx.Configuration.TypeSystem.GetType("Avalonia.Data.IBinding");
            IDisposable = ctx.Configuration.TypeSystem.GetType("System.IDisposable");
            Transitions = ctx.Configuration.TypeSystem.GetType("Avalonia.Animation.Transitions");
            AssignBindingAttribute = ctx.Configuration.TypeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            AvaloniaObjectBindMethod = AvaloniaObjectExtensions.FindMethod("Bind", IDisposable, false, IAvaloniaObject,
                AvaloniaProperty,
                IBinding, ctx.Configuration.WellKnownTypes.Object);
            UnsetValueType = ctx.Configuration.TypeSystem.GetType("Avalonia.UnsetValueType");
            StyledElement = ctx.Configuration.TypeSystem.GetType("Avalonia.StyledElement");
            NameScope = ctx.Configuration.TypeSystem.GetType("Avalonia.Controls.NameScope");
            NameScopeStaticRegister = NameScope.FindMethod(
                new FindMethodMethodSignature("Register", XamlIlTypes.Void,
                     StyledElement, XamlIlTypes.String, XamlIlTypes.Object)
                {
                    IsStatic = true, DeclaringOnly = true, IsExactMatch = true
                });

            AvaloniaObjectSetValueMethod = AvaloniaObject.FindMethod("SetValue", XamlIlTypes.Void,
                false, AvaloniaProperty, XamlIlTypes.Object, BindingPriority);
        }
    }

    static class AvaloniaXamlIlWellKnownTypesExtensions
    {
        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this XamlIlAstTransformationContext ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx));
            return rv;
        }
    }
}
