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
        public IXamlType IPropertyInfo { get; }
        public IXamlType ClrPropertyInfo { get; }
        public IXamlType PropertyPath { get; }
        public IXamlType PropertyPathBuilder { get; }
        public IXamlType IPropertyAccessor { get; }
        public IXamlType PropertyInfoAccessorFactory { get; }
        public IXamlType CompiledBindingPathBuilder { get; }
        public IXamlType CompiledBindingPath { get; }
        public IXamlType CompiledBindingExtension { get; }

        public IXamlType ResolveByNameExtension { get; }

        public IXamlType DataTemplate { get; }
        public IXamlType IDataTemplate { get; }
        public IXamlType IItemsPresenterHost { get; }
        public IXamlType ItemsRepeater { get; }
        public IXamlType ReflectionBindingExtension { get; }

        public IXamlType RelativeSource { get; }

        public AvaloniaXamlIlWellKnownTypes(TransformerConfiguration cfg)
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
            ResolveByNameExtension = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.ResolveByNameExtension");
            DataTemplate = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Templates.DataTemplate");
            IDataTemplate = cfg.TypeSystem.GetType("Avalonia.Controls.Templates.IDataTemplate");
            IItemsPresenterHost = cfg.TypeSystem.GetType("Avalonia.Controls.Presenters.IItemsPresenterHost");
            ItemsRepeater = cfg.TypeSystem.GetType("Avalonia.Controls.ItemsRepeater");
            ReflectionBindingExtension = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.ReflectionBindingExtension");
            RelativeSource = cfg.TypeSystem.GetType("Avalonia.Data.RelativeSource");
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
