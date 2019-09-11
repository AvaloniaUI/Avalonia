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
        public XamlIlLanguageTypeMappings XamlIlMappings { get; }
        public IXamlIlType Transitions { get; }
        public IXamlIlType AssignBindingAttribute { get; }
        public IXamlIlType UnsetValueType { get; }
        public IXamlIlType StyledElement { get; }
        public IXamlIlType NameScope { get; }
        public IXamlIlMethod NameScopeSetNameScope { get; }
        public IXamlIlType INameScope { get; }
        public IXamlIlMethod INameScopeRegister { get; }
        public IXamlIlMethod INameScopeComplete { get; }
        public IXamlIlType IPropertyInfo { get; }
        public IXamlIlType ClrPropertyInfo { get; }
        public IXamlIlType PropertyPath { get; }
        public IXamlIlType PropertyPathBuilder { get; }
        public IXamlIlType IPropertyAccessor { get; }
        public IXamlIlType PropertyInfoAccessorFactory { get; }
        public IXamlIlType CompiledBindingPathBuilder { get; }
        public IXamlIlType CompiledBindingPath { get; }
        public IXamlIlType CompiledBindingExtension { get; }
        public IXamlIlType DataTemplate { get; }
        public IXamlIlType IItemsPresenterHost { get; }

        public AvaloniaXamlIlWellKnownTypes(XamlIlTransformerConfiguration cfg)
        {
            XamlIlTypes = cfg.WellKnownTypes;
            AvaloniaObject = cfg.TypeSystem.GetType("Avalonia.AvaloniaObject");
            IAvaloniaObject = cfg.TypeSystem.GetType("Avalonia.IAvaloniaObject");
            AvaloniaObjectExtensions = cfg.TypeSystem.GetType("Avalonia.AvaloniaObjectExtensions");
            AvaloniaProperty = cfg.TypeSystem.GetType("Avalonia.AvaloniaProperty");
            AvaloniaPropertyT = cfg.TypeSystem.GetType("Avalonia.AvaloniaProperty`1");
            BindingPriority = cfg.TypeSystem.GetType("Avalonia.Data.BindingPriority");
            IBinding = cfg.TypeSystem.GetType("Avalonia.Data.IBinding");
            IDisposable = cfg.TypeSystem.GetType("System.IDisposable");
            Transitions = cfg.TypeSystem.GetType("Avalonia.Animation.Transitions");
            AssignBindingAttribute = cfg.TypeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            AvaloniaObjectBindMethod = AvaloniaObjectExtensions.FindMethod("Bind", IDisposable, false, IAvaloniaObject,
                AvaloniaProperty,
                IBinding, cfg.WellKnownTypes.Object);
            UnsetValueType = cfg.TypeSystem.GetType("Avalonia.UnsetValueType");
            StyledElement = cfg.TypeSystem.GetType("Avalonia.StyledElement");
            INameScope = cfg.TypeSystem.GetType("Avalonia.Controls.INameScope");
            INameScopeRegister = INameScope.GetMethod(
                new FindMethodMethodSignature("Register", XamlIlTypes.Void,
                     XamlIlTypes.String, XamlIlTypes.Object)
                {
                    IsStatic = false,
                    DeclaringOnly = true,
                    IsExactMatch = true
                });
            INameScopeComplete = INameScope.GetMethod(
                new FindMethodMethodSignature("Complete", XamlIlTypes.Void)
                {
                    IsStatic = false,
                    DeclaringOnly = true,
                    IsExactMatch = true
                });
            NameScope = cfg.TypeSystem.GetType("Avalonia.Controls.NameScope");
            NameScopeSetNameScope = NameScope.GetMethod(new FindMethodMethodSignature("SetNameScope",
                XamlIlTypes.Void, StyledElement, INameScope)
            { IsStatic = true });
            AvaloniaObjectSetValueMethod = AvaloniaObject.FindMethod("SetValue", XamlIlTypes.Void,
                false, AvaloniaProperty, XamlIlTypes.Object, BindingPriority);
            IPropertyInfo = cfg.TypeSystem.GetType("Avalonia.Data.Core.IPropertyInfo");
            ClrPropertyInfo = cfg.TypeSystem.GetType("Avalonia.Data.Core.ClrPropertyInfo");
            PropertyPath = cfg.TypeSystem.GetType("Avalonia.Data.Core.PropertyPath");
            PropertyPathBuilder = cfg.TypeSystem.GetType("Avalonia.Data.Core.PropertyPathBuilder");
            IPropertyAccessor = cfg.TypeSystem.GetType("Avalonia.Data.Core.Plugins.IPropertyAccessor");
            PropertyInfoAccessorFactory = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.PropertyInfoAccessorFactory");
            CompiledBindingPathBuilder = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.CompiledBindingPathBuilder");
            CompiledBindingPath = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.CompiledBindingPath");
            CompiledBindingExtension = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindingExtension");
            DataTemplate = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Templates.DataTemplate");
            IItemsPresenterHost = cfg.TypeSystem.GetType("Avalonia.Controls.Presenters.IItemsPresenterHost");
        }
    }

    static class AvaloniaXamlIlWellKnownTypesExtensions
    {
        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this XamlIlAstTransformationContext ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }
        
        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this XamlIlEmitContext ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }
    }
}
