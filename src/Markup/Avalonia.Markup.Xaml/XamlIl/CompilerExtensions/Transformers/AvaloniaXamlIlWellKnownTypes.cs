using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlWellKnownTypes
    {
        public IXamlIlType AvaloniaObject { get; }
        public IXamlIlType IAvaloniaObject { get; }
        public IXamlIlType AvaloniaObjectExtensions { get; }
        public IXamlIlType AvaloniaProperty { get; }
        public IXamlIlType IBinding { get; }
        public IXamlIlMethod AvaloniaObjectBindMethod { get; }
        public IXamlIlType IDisposable { get; }
        public XamlIlTypeWellKnownTypes XamlIlTypes { get; }
        public IXamlIlType Transitions { get; }
        public IXamlIlType AssignBindingAttribute { get; }
        
        public AvaloniaXamlIlWellKnownTypes(XamlIlAstTransformationContext ctx)
        {
            XamlIlTypes = ctx.Configuration.WellKnownTypes;
            AvaloniaObject = ctx.Configuration.TypeSystem.GetType("Avalonia.AvaloniaObject");
            IAvaloniaObject = ctx.Configuration.TypeSystem.GetType("Avalonia.IAvaloniaObject");
            AvaloniaObjectExtensions = ctx.Configuration.TypeSystem.GetType("Avalonia.AvaloniaObjectExtensions");
            AvaloniaProperty = ctx.Configuration.TypeSystem.GetType("Avalonia.AvaloniaProperty");
            IBinding = ctx.Configuration.TypeSystem.GetType("Avalonia.Data.IBinding");
            IDisposable = ctx.Configuration.TypeSystem.GetType("System.IDisposable");
            Transitions = ctx.Configuration.TypeSystem.GetType("Avalonia.Animation.Transitions");
            AssignBindingAttribute = ctx.Configuration.TypeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            AvaloniaObjectBindMethod = AvaloniaObjectExtensions.FindMethod("Bind", IDisposable, false, IAvaloniaObject,
                AvaloniaProperty,
                IBinding, ctx.Configuration.WellKnownTypes.Object);
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
