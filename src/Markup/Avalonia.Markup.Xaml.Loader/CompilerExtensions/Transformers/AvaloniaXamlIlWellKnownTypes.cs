using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlWellKnownTypes
    {
        public IXamlType RuntimeHelpers { get; }
        public IXamlType AvaloniaObject { get; }
        public IXamlType BindingPriority { get; }
        public IXamlType AvaloniaObjectExtensions { get; }
        public IXamlType AvaloniaProperty { get; }
        public IXamlType AvaloniaPropertyT { get; }
        public IXamlType StyledPropertyT { get; }
        public IXamlMethod AvaloniaObjectSetStyledPropertyValue { get; }
        public IXamlType AvaloniaAttachedPropertyT { get; }
        public IXamlType IBinding { get; }
        public IXamlMethod AvaloniaObjectBindMethod { get; }
        public IXamlMethod AvaloniaObjectSetValueMethod { get; }
        public IXamlType IDisposable { get; }
        public IXamlType ICommand { get; }
        public XamlTypeWellKnownTypes XamlIlTypes { get; }
        public XamlLanguageTypeMappings XamlIlMappings { get; }
        public IXamlType Transitions { get; }
        public IXamlType AssignBindingAttribute { get; }
        public IXamlType DependsOnAttribute { get; }
        public IXamlType DataTypeAttribute { get; }
        public IXamlType InheritDataTypeFromItemsAttribute { get; }
        public IXamlType MarkupExtensionOptionAttribute { get; }
        public IXamlType MarkupExtensionDefaultOptionAttribute { get; }
        public IXamlType AvaloniaListAttribute { get; }
        public IXamlType AvaloniaList { get; }
        public IXamlType OnExtensionType { get; }
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
        public IXamlType ItemsControl { get; }
        public IXamlType ReflectionBindingExtension { get; }

        public IXamlType RelativeSource { get; }
        public IXamlType UInt { get; }
        public IXamlType Int { get; }
        public IXamlType Long { get; }
        public IXamlType Uri { get; }
        public IXamlType IDictionaryT { get; }
        public IXamlType FontFamily { get; }
        public IXamlConstructor FontFamilyConstructorUriName { get; }
        public IXamlType Thickness { get; }
        public IXamlConstructor ThicknessFullConstructor { get; }
        public IXamlType ThemeVariant { get; }
        public IXamlType Point { get; }
        public IXamlConstructor PointFullConstructor { get; }
        public IXamlType Vector { get; }
        public IXamlConstructor VectorFullConstructor { get; }
        public IXamlType Size { get; }
        public IXamlConstructor SizeFullConstructor { get; }
        public IXamlType Matrix { get; }
        public IXamlConstructor MatrixFullConstructor { get; }
        public IXamlType CornerRadius { get; }
        public IXamlConstructor CornerRadiusFullConstructor { get; }
        public IXamlType RelativeUnit { get; }
        public IXamlType RelativePoint { get; }
        public IXamlConstructor RelativePointFullConstructor { get; }
        public IXamlType GridLength { get; }
        public IXamlConstructor GridLengthConstructorValueType { get; }
        public IXamlType Color { get; }
        public IXamlType StandardCursorType { get; }
        public IXamlType Cursor { get; }
        public IXamlConstructor CursorTypeConstructor { get; }
        public IXamlType RowDefinition { get; }
        public IXamlType RowDefinitions { get; }
        public IXamlType ColumnDefinition { get; }
        public IXamlType ColumnDefinitions { get; }
        public IXamlType Classes { get; }
        public IXamlMethod ClassesBindMethod { get; }
        public IXamlProperty StyledElementClassesProperty { get; }
        public IXamlType IBrush { get; }
        public IXamlType ImmutableSolidColorBrush { get; }
        public IXamlConstructor ImmutableSolidColorBrushConstructorColor { get; }
        public IXamlType TypeUtilities { get; }
        public IXamlType TextDecorationCollection { get; }
        public IXamlType TextDecorations { get; }
        public IXamlType TextTrimming { get; }
        public IXamlType SetterBase { get; }
        public IXamlType IStyle { get; }
        public IXamlType StyleInclude { get; }
        public IXamlType ResourceInclude { get; }
        public IXamlType MergeResourceInclude { get; }
        public IXamlType IResourceDictionary { get; }
        public IXamlType ResourceDictionary { get; }
        public IXamlMethod ResourceDictionaryDeferredAdd { get; }
        public IXamlType IThemeVariantProvider { get; }
        public IXamlType UriKind { get; }
        public IXamlConstructor UriConstructor { get; }
        public IXamlType Style { get; }
        public IXamlType ControlTheme { get; }
        public IXamlType WindowTransparencyLevel { get; }
        public IXamlType IReadOnlyListOfT { get; }

        public AvaloniaXamlIlWellKnownTypes(TransformerConfiguration cfg)
        {
            RuntimeHelpers = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");

            XamlIlTypes = cfg.WellKnownTypes;
            AvaloniaObject = cfg.TypeSystem.GetType("Avalonia.AvaloniaObject");
            AvaloniaObjectExtensions = cfg.TypeSystem.GetType("Avalonia.AvaloniaObjectExtensions");
            AvaloniaProperty = cfg.TypeSystem.GetType("Avalonia.AvaloniaProperty");
            AvaloniaPropertyT = cfg.TypeSystem.GetType("Avalonia.AvaloniaProperty`1");
            StyledPropertyT = cfg.TypeSystem.GetType("Avalonia.StyledProperty`1");
            AvaloniaAttachedPropertyT = cfg.TypeSystem.GetType("Avalonia.AttachedProperty`1");
            BindingPriority = cfg.TypeSystem.GetType("Avalonia.Data.BindingPriority");
            AvaloniaObjectSetStyledPropertyValue = AvaloniaObject
                .FindMethod(m => m.IsPublic && !m.IsStatic && m.Name == "SetValue"
                                 && m.Parameters.Count == 3
                                 && m.Parameters[0].Name == "StyledProperty`1"
                                 && m.Parameters[2].Equals(BindingPriority));
            IBinding = cfg.TypeSystem.GetType("Avalonia.Data.IBinding");
            IDisposable = cfg.TypeSystem.GetType("System.IDisposable");
            ICommand = cfg.TypeSystem.GetType("System.Windows.Input.ICommand");
            Transitions = cfg.TypeSystem.GetType("Avalonia.Animation.Transitions");
            AssignBindingAttribute = cfg.TypeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            DependsOnAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.DependsOnAttribute");
            DataTypeAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.DataTypeAttribute");
            InheritDataTypeFromItemsAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.InheritDataTypeFromItemsAttribute");
            MarkupExtensionOptionAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.MarkupExtensionOptionAttribute");
            MarkupExtensionDefaultOptionAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.MarkupExtensionDefaultOptionAttribute");
            AvaloniaListAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.AvaloniaListAttribute");
            AvaloniaList = cfg.TypeSystem.GetType("Avalonia.Collections.AvaloniaList`1");
            OnExtensionType = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.On");
            AvaloniaObjectBindMethod = AvaloniaObjectExtensions.FindMethod("Bind", IDisposable, false, AvaloniaObject,
                AvaloniaProperty,
                IBinding, cfg.WellKnownTypes.Object);
            UnsetValueType = cfg.TypeSystem.GetType("Avalonia.UnsetValueType");
            StyledElement = cfg.TypeSystem.GetType("Avalonia.StyledElement");
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
            AvaloniaObjectSetValueMethod = AvaloniaObject.FindMethod("SetValue", IDisposable,
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
            ItemsControl = cfg.TypeSystem.GetType("Avalonia.Controls.ItemsControl");
            ReflectionBindingExtension = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.ReflectionBindingExtension");
            RelativeSource = cfg.TypeSystem.GetType("Avalonia.Data.RelativeSource");
            UInt = cfg.TypeSystem.GetType("System.UInt32");
            Int = cfg.TypeSystem.GetType("System.Int32");
            Long = cfg.TypeSystem.GetType("System.Int64");
            Uri = cfg.TypeSystem.GetType("System.Uri");
            IDictionaryT = cfg.TypeSystem.GetType("System.Collections.Generic.IDictionary`2");
            FontFamily = cfg.TypeSystem.GetType("Avalonia.Media.FontFamily");
            FontFamilyConstructorUriName = FontFamily.GetConstructor(new List<IXamlType> { Uri, XamlIlTypes.String });
            ThemeVariant = cfg.TypeSystem.GetType("Avalonia.Styling.ThemeVariant");
            WindowTransparencyLevel = cfg.TypeSystem.GetType("Avalonia.Controls.WindowTransparencyLevel");

            (IXamlType, IXamlConstructor) GetNumericTypeInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string name, IXamlType componentType, int componentCount)
            {
                var type = cfg.TypeSystem.GetType(name);
                var ctor = type.GetConstructor(Enumerable.Range(0, componentCount).Select(_ => componentType).ToList());

                return (type, ctor);
            }

            (Thickness, ThicknessFullConstructor) = GetNumericTypeInfo("Avalonia.Thickness", XamlIlTypes.Double, 4);
            (Point, PointFullConstructor) = GetNumericTypeInfo("Avalonia.Point", XamlIlTypes.Double, 2);
            (Vector, VectorFullConstructor) = GetNumericTypeInfo("Avalonia.Vector", XamlIlTypes.Double, 2);
            (Size, SizeFullConstructor) = GetNumericTypeInfo("Avalonia.Size", XamlIlTypes.Double, 2);
            (Matrix, MatrixFullConstructor) = GetNumericTypeInfo("Avalonia.Matrix", XamlIlTypes.Double, 6);
            (CornerRadius, CornerRadiusFullConstructor) = GetNumericTypeInfo("Avalonia.CornerRadius", XamlIlTypes.Double, 4);

            RelativeUnit = cfg.TypeSystem.GetType("Avalonia.RelativeUnit");
            RelativePoint = cfg.TypeSystem.GetType("Avalonia.RelativePoint");
            RelativePointFullConstructor = RelativePoint.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, XamlIlTypes.Double, RelativeUnit });

            GridLength = cfg.TypeSystem.GetType("Avalonia.Controls.GridLength");
            GridLengthConstructorValueType = GridLength.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, cfg.TypeSystem.GetType("Avalonia.Controls.GridUnitType") });
            Color = cfg.TypeSystem.GetType("Avalonia.Media.Color");
            StandardCursorType = cfg.TypeSystem.GetType("Avalonia.Input.StandardCursorType");
            Cursor = cfg.TypeSystem.GetType("Avalonia.Input.Cursor");
            CursorTypeConstructor = Cursor.GetConstructor(new List<IXamlType> { StandardCursorType });
            ColumnDefinition = cfg.TypeSystem.GetType("Avalonia.Controls.ColumnDefinition");
            ColumnDefinitions = cfg.TypeSystem.GetType("Avalonia.Controls.ColumnDefinitions");
            RowDefinition = cfg.TypeSystem.GetType("Avalonia.Controls.RowDefinition");
            RowDefinitions = cfg.TypeSystem.GetType("Avalonia.Controls.RowDefinitions");
            Classes = cfg.TypeSystem.GetType("Avalonia.Controls.Classes");
            StyledElementClassesProperty =
                StyledElement.Properties.First(x => x.Name == "Classes" && x.PropertyType.Equals(Classes));
            ClassesBindMethod = cfg.TypeSystem.GetType("Avalonia.StyledElementExtensions")
                .FindMethod( "BindClass", IDisposable, false, StyledElement,
                cfg.WellKnownTypes.String,
                IBinding, cfg.WellKnownTypes.Object);

            IBrush = cfg.TypeSystem.GetType("Avalonia.Media.IBrush");
            ImmutableSolidColorBrush = cfg.TypeSystem.GetType("Avalonia.Media.Immutable.ImmutableSolidColorBrush");
            ImmutableSolidColorBrushConstructorColor = ImmutableSolidColorBrush.GetConstructor(new List<IXamlType> { UInt });
            TypeUtilities = cfg.TypeSystem.GetType("Avalonia.Utilities.TypeUtilities");
            TextDecorationCollection = cfg.TypeSystem.GetType("Avalonia.Media.TextDecorationCollection");
            TextDecorations = cfg.TypeSystem.GetType("Avalonia.Media.TextDecorations");
            TextTrimming = cfg.TypeSystem.GetType("Avalonia.Media.TextTrimming");
            SetterBase = cfg.TypeSystem.GetType("Avalonia.Styling.SetterBase");
            IStyle = cfg.TypeSystem.GetType("Avalonia.Styling.IStyle");
            StyleInclude = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Styling.StyleInclude");
            ResourceInclude = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Styling.ResourceInclude");
            MergeResourceInclude = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Styling.MergeResourceInclude");
            IResourceDictionary = cfg.TypeSystem.GetType("Avalonia.Controls.IResourceDictionary");
            ResourceDictionary = cfg.TypeSystem.GetType("Avalonia.Controls.ResourceDictionary");
            ResourceDictionaryDeferredAdd = ResourceDictionary.FindMethod("AddDeferred", XamlIlTypes.Void, true, XamlIlTypes.Object,
                cfg.TypeSystem.GetType("System.Func`2").MakeGenericType(
                    cfg.TypeSystem.GetType("System.IServiceProvider"),
                    XamlIlTypes.Object));
            IThemeVariantProvider = cfg.TypeSystem.GetType("Avalonia.Controls.IThemeVariantProvider");
            UriKind = cfg.TypeSystem.GetType("System.UriKind");
            UriConstructor = Uri.GetConstructor(new List<IXamlType>() { cfg.WellKnownTypes.String, UriKind });
            Style = cfg.TypeSystem.GetType("Avalonia.Styling.Style");
            ControlTheme = cfg.TypeSystem.GetType("Avalonia.Styling.ControlTheme");
            IReadOnlyListOfT = cfg.TypeSystem.GetType("System.Collections.Generic.IReadOnlyList`1");
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
        
        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this AstGroupTransformationContext ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }
    }
}
