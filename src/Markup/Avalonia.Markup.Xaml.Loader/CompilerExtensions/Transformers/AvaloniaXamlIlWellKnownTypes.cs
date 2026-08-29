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

    sealed class AvaloniaXamlIlWellKnownTypes
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
        public IXamlType BindingBase { get; }
        public IXamlType BindingExpressionBase { get; }
        public IXamlType MultiBinding { get; }
        public IXamlMethod AvaloniaObjectBindMethod { get; }
        public IXamlMethod AvaloniaObjectSetValueMethod { get; }
        public IXamlType IDisposable { get; }
        public IXamlType ICommand { get; }
        public XamlTypeWellKnownTypes XamlIlTypes { get; }
        public IXamlType Transitions { get; }
        public IXamlType AssignBindingAttribute { get; }
        public IXamlType DependsOnAttribute { get; }
        public IXamlType DataTypeAttribute { get; }
        public IXamlType InheritDataTypeFromItemsAttribute { get; }
        public IXamlType InheritDataTypeFromAttribute { get; }
        public IXamlType MarkupExtensionOptionAttribute { get; }
        public IXamlType MarkupExtensionDefaultOptionAttribute { get; }
        public IXamlType ControlTemplateScopeAttribute { get; }
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
        public IXamlType IPropertyInfoT { get; }
        public IXamlType ClrPropertyInfoT { get; }
        public IXamlType IPropertyAccessor { get; }
        public IXamlType PropertyInfoAccessorFactory { get; }
        public IXamlType CompiledBinding { get; }
        public IXamlType CompiledBindingPathBuilder { get; }
        public IXamlType CompiledBindingPath { get; }
        public IXamlType CompiledBindingExtension { get; }

        public IXamlType ResolveByNameExtension { get; }

        public IXamlType DataTemplate { get; }
        public IXamlType IDataTemplate { get; }
        public IXamlType ITemplateOfControl { get; }
        public IXamlType Control { get; }
        public IXamlType ContentControl { get; }
        public IXamlType ItemsControl { get; }
        public IXamlType ReflectionBindingExtension { get; }

        public IXamlType RelativeSource { get; }
        public IXamlType UInt { get; }
        public IXamlType Int { get; }
        public IXamlType Long { get; }
        public IXamlType Uri { get; }
        public IXamlType TaskOfT { get; }
        public IXamlType IDictionaryT { get; }
        public IXamlType WeakReferenceOfT { get; }
        public IXamlType IObservableOfT { get; }
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
        public IXamlType Setter { get; }
        public IXamlType IStyle { get; }
        public IXamlType StyleInclude { get; }
        public IXamlType ResourceInclude { get; }
        public IXamlType MergeResourceInclude { get; }
        public IXamlType IResourceDictionary { get; }
        public IXamlType ResourceDictionary { get; }
        public IXamlMethod ResourceDictionaryDeferredAdd { get; }
        public IXamlMethod ResourceDictionaryNotSharedDeferredAdd { get; }
        public IXamlMethod ResourceDictionaryEnsureCapacity { get; }
        public IXamlMethod ResourceDictionaryGetCount { get; }
        public IXamlType IThemeVariantProvider { get; }
        public IXamlType UriKind { get; }
        public IXamlConstructor UriConstructor { get; }
        public IXamlType Style { get; }
        public IXamlType Container { get; }
        public IXamlType Styles { get; }
        public IXamlType StyleQueries { get; }
        public IXamlType Selectors { get; }
        public IXamlType ControlTheme { get; }
        public IXamlType WindowTransparencyLevel { get; }
        public IXamlType IReadOnlyListOfT { get; }
        public IXamlType ControlTemplate { get; }
        public IXamlType EventHandlerT {  get; }
        public IXamlMethod GetClassProperty { get; }
        public IXamlConstructor XamlSourceInfoConstructor { get; }
        public IXamlMethod XamlSourceInfoSetter { get; }
        public IXamlMethod XamlSourceInfoDictionarySetter { get; }

        sealed internal class InteractivityWellKnownTypes
        {
            public IXamlType Interactive { get; }
            public IXamlType RoutedEvent { get; }
            public IXamlType RoutedEventArgs { get; }
            public IXamlType RoutedEventHandler { get; }
            public IXamlMethod AddHandler { get; }
            public IXamlMethod AddHandlerT { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
            internal InteractivityWellKnownTypes(IXamlTypeSystem typeSystem, XamlTypeWellKnownTypes wellKnownTypes)
            {
                Interactive = typeSystem.GetType("Avalonia.Interactivity.Interactive");
                RoutedEvent = typeSystem.GetType("Avalonia.Interactivity.RoutedEvent");
                RoutedEventArgs = typeSystem.GetType("Avalonia.Interactivity.RoutedEventArgs");
                var eventHanlderT = typeSystem.GetType("System.EventHandler`1");
                RoutedEventHandler = eventHanlderT.MakeGenericType(RoutedEventArgs);
                AddHandler = Interactive.GetMethod(m => m.IsPublic
                    && !m.IsStatic
                    && m.Name == "AddHandler"
                    && m.Parameters.Count == 4
                    && m.Parameters[0].Equals(RoutedEvent)
                    && m.Parameters[1].Equals(wellKnownTypes.Delegate)
                    && m.Parameters[2].IsEnum
                    && m.Parameters[3].Equals(wellKnownTypes.Boolean)
                    );
                AddHandlerT = Interactive.GetMethod(m => m.IsPublic
                    && !m.IsStatic
                    && m.Name == "AddHandler"
                    && m.Parameters.Count == 4
                    && RoutedEvent.IsAssignableFrom(m.Parameters[0])
                    && m.Parameters[0].GenericArguments.Count == 1 // This is specific this case  workaround to check is generic method
                    && wellKnownTypes.Delegate.IsAssignableFrom(m.Parameters[1])
                    && m.Parameters[2].IsEnum
                    && m.Parameters[3].Equals(wellKnownTypes.Boolean)
                );

            }
        }

        public InteractivityWellKnownTypes Interactivity { get; }

        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
        public AvaloniaXamlIlWellKnownTypes(IXamlTypeSystem typeSystem)
        {
            RuntimeHelpers = typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");

            XamlIlTypes = typeSystem.WellKnownTypes;
            AvaloniaObject = typeSystem.GetType("Avalonia.AvaloniaObject");
            AvaloniaObjectExtensions = typeSystem.GetType("Avalonia.AvaloniaObjectExtensions");
            AvaloniaProperty = typeSystem.GetType("Avalonia.AvaloniaProperty");
            AvaloniaPropertyT = typeSystem.GetType("Avalonia.AvaloniaProperty`1");
            StyledPropertyT = typeSystem.GetType("Avalonia.StyledProperty`1");
            AvaloniaAttachedPropertyT = typeSystem.GetType("Avalonia.AttachedProperty`1");
            BindingPriority = typeSystem.GetType("Avalonia.Data.BindingPriority");
            AvaloniaObjectSetStyledPropertyValue = AvaloniaObject
                .GetMethod(m => m.IsPublic && !m.IsStatic && m.Name == "SetValue"
                                 && m.Parameters.Count == 3
                                 && m.Parameters[0].Name == "StyledProperty`1"
                                 && m.Parameters[2].Equals(BindingPriority));
            BindingBase = typeSystem.GetType("Avalonia.Data.BindingBase");
            BindingExpressionBase = typeSystem.GetType("Avalonia.Data.BindingExpressionBase");
            MultiBinding = typeSystem.GetType("Avalonia.Data.MultiBinding");
            IDisposable = typeSystem.GetType("System.IDisposable");
            ICommand = typeSystem.GetType("System.Windows.Input.ICommand");
            Transitions = typeSystem.GetType("Avalonia.Animation.Transitions");
            AssignBindingAttribute = typeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            DependsOnAttribute = typeSystem.GetType("Avalonia.Metadata.DependsOnAttribute");
            DataTypeAttribute = typeSystem.GetType("Avalonia.Metadata.DataTypeAttribute");
            InheritDataTypeFromItemsAttribute = typeSystem.GetType("Avalonia.Metadata.InheritDataTypeFromItemsAttribute");
            InheritDataTypeFromAttribute = typeSystem.GetType("Avalonia.Metadata.InheritDataTypeFromAttribute");
            MarkupExtensionOptionAttribute = typeSystem.GetType("Avalonia.Metadata.MarkupExtensionOptionAttribute");
            MarkupExtensionDefaultOptionAttribute = typeSystem.GetType("Avalonia.Metadata.MarkupExtensionDefaultOptionAttribute");
            ControlTemplateScopeAttribute = typeSystem.GetType("Avalonia.Metadata.ControlTemplateScopeAttribute");
            AvaloniaListAttribute = typeSystem.GetType("Avalonia.Metadata.AvaloniaListAttribute");
            AvaloniaList = typeSystem.GetType("Avalonia.Collections.AvaloniaList`1");
            OnExtensionType = typeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.On");
            AvaloniaObjectBindMethod = AvaloniaObject.GetMethod("Bind", BindingExpressionBase, false, AvaloniaProperty, BindingBase);
            UnsetValueType = typeSystem.GetType("Avalonia.UnsetValueType");
            StyledElement = typeSystem.GetType("Avalonia.StyledElement");
            INameScope = typeSystem.GetType("Avalonia.Controls.INameScope");
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
            NameScope = typeSystem.GetType("Avalonia.Controls.NameScope");
            NameScopeSetNameScope = NameScope.GetMethod(new FindMethodMethodSignature("SetNameScope",
                XamlIlTypes.Void, StyledElement, INameScope)
            { IsStatic = true });
            AvaloniaObjectSetValueMethod = AvaloniaObject.GetMethod("SetValue", IDisposable,
                false, AvaloniaProperty, XamlIlTypes.Object, BindingPriority);
            IPropertyInfo = typeSystem.GetType("Avalonia.Data.Core.IPropertyInfo");
            ClrPropertyInfo = typeSystem.GetType("Avalonia.Data.Core.ClrPropertyInfo");
            IPropertyInfoT = typeSystem.GetType("Avalonia.Data.Core.IPropertyInfo`2");
            ClrPropertyInfoT = typeSystem.GetType("Avalonia.Data.Core.ClrPropertyInfo`2");
            IPropertyAccessor = typeSystem.GetType("Avalonia.Data.Core.Plugins.IPropertyAccessor");
            PropertyInfoAccessorFactory = typeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.PropertyInfoAccessorFactory");
            CompiledBinding = typeSystem.GetType("Avalonia.Data.CompiledBinding");
            CompiledBindingPathBuilder = typeSystem.GetType("Avalonia.Data.CompiledBindingPathBuilder");
            CompiledBindingPath = typeSystem.GetType("Avalonia.Data.CompiledBindingPath");
            CompiledBindingExtension = typeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindingExtension");
            ResolveByNameExtension = typeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.ResolveByNameExtension");
            DataTemplate = typeSystem.GetType("Avalonia.Markup.Xaml.Templates.DataTemplate");
            IDataTemplate = typeSystem.GetType("Avalonia.Controls.Templates.IDataTemplate");
            Control = typeSystem.GetType("Avalonia.Controls.Control");
            ContentControl = typeSystem.GetType("Avalonia.Controls.ContentControl");
            ITemplateOfControl = typeSystem.GetType("Avalonia.Controls.ITemplate`1").MakeGenericType(Control);
            ItemsControl = typeSystem.GetType("Avalonia.Controls.ItemsControl");
            ReflectionBindingExtension = typeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.ReflectionBindingExtension");
            RelativeSource = typeSystem.GetType("Avalonia.Data.RelativeSource");
            UInt = typeSystem.GetType("System.UInt32");
            Int = typeSystem.GetType("System.Int32");
            Long = typeSystem.GetType("System.Int64");
            Uri = typeSystem.GetType("System.Uri");
            TaskOfT = typeSystem.GetType("System.Threading.Tasks.Task`1");
            IDictionaryT = typeSystem.GetType("System.Collections.Generic.IDictionary`2");
            WeakReferenceOfT = typeSystem.GetType("System.WeakReference`1");
            IObservableOfT = typeSystem.GetType("System.IObservable`1");
            FontFamily = typeSystem.GetType("Avalonia.Media.FontFamily");
            FontFamilyConstructorUriName = FontFamily.GetConstructor(new List<IXamlType> { Uri, XamlIlTypes.String });
            ThemeVariant = typeSystem.GetType("Avalonia.Styling.ThemeVariant");
            WindowTransparencyLevel = typeSystem.GetType("Avalonia.Controls.WindowTransparencyLevel");

            (IXamlType, IXamlConstructor) GetNumericTypeInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string name, IXamlType componentType, int componentCount)
            {
                var type = typeSystem.GetType(name);
                var ctor = type.GetConstructor(Enumerable.Range(0, componentCount).Select(_ => componentType).ToList());

                return (type, ctor);
            }

            (Thickness, ThicknessFullConstructor) = GetNumericTypeInfo("Avalonia.Thickness", XamlIlTypes.Double, 4);
            (Point, PointFullConstructor) = GetNumericTypeInfo("Avalonia.Point", XamlIlTypes.Double, 2);
            (Vector, VectorFullConstructor) = GetNumericTypeInfo("Avalonia.Vector", XamlIlTypes.Double, 2);
            (Size, SizeFullConstructor) = GetNumericTypeInfo("Avalonia.Size", XamlIlTypes.Double, 2);
            (Matrix, MatrixFullConstructor) = GetNumericTypeInfo("Avalonia.Matrix", XamlIlTypes.Double, 6);
            (CornerRadius, CornerRadiusFullConstructor) = GetNumericTypeInfo("Avalonia.CornerRadius", XamlIlTypes.Double, 4);

            RelativeUnit = typeSystem.GetType("Avalonia.RelativeUnit");
            RelativePoint = typeSystem.GetType("Avalonia.RelativePoint");
            RelativePointFullConstructor = RelativePoint.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, XamlIlTypes.Double, RelativeUnit });

            GridLength = typeSystem.GetType("Avalonia.Controls.GridLength");
            GridLengthConstructorValueType = GridLength.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, typeSystem.GetType("Avalonia.Controls.GridUnitType") });
            Color = typeSystem.GetType("Avalonia.Media.Color");
            StandardCursorType = typeSystem.GetType("Avalonia.Input.StandardCursorType");
            Cursor = typeSystem.GetType("Avalonia.Input.Cursor");
            CursorTypeConstructor = Cursor.GetConstructor(new List<IXamlType> { StandardCursorType });
            ColumnDefinition = typeSystem.GetType("Avalonia.Controls.ColumnDefinition");
            ColumnDefinitions = typeSystem.GetType("Avalonia.Controls.ColumnDefinitions");
            RowDefinition = typeSystem.GetType("Avalonia.Controls.RowDefinition");
            RowDefinitions = typeSystem.GetType("Avalonia.Controls.RowDefinitions");
            Classes = typeSystem.GetType("Avalonia.Controls.Classes");
            StyledElementClassesProperty =
                StyledElement.Properties.First(x => x.Name == "Classes" && x.PropertyType.Equals(Classes));
            ClassesBindMethod = typeSystem.GetType("Avalonia.StyledElementExtensions")
                .GetMethod("BindClass", IDisposable, false, StyledElement,
                typeSystem.WellKnownTypes.String,
                BindingBase, typeSystem.WellKnownTypes.Object);

            IBrush = typeSystem.GetType("Avalonia.Media.IBrush");
            ImmutableSolidColorBrush = typeSystem.GetType("Avalonia.Media.Immutable.ImmutableSolidColorBrush");
            ImmutableSolidColorBrushConstructorColor = ImmutableSolidColorBrush.GetConstructor(new List<IXamlType> { UInt });
            TypeUtilities = typeSystem.GetType("Avalonia.Utilities.TypeUtilities");
            TextDecorationCollection = typeSystem.GetType("Avalonia.Media.TextDecorationCollection");
            TextDecorations = typeSystem.GetType("Avalonia.Media.TextDecorations");
            TextTrimming = typeSystem.GetType("Avalonia.Media.TextTrimming");
            SetterBase = typeSystem.GetType("Avalonia.Styling.SetterBase");
            Setter = typeSystem.GetType("Avalonia.Styling.Setter");
            IStyle = typeSystem.GetType("Avalonia.Styling.IStyle");
            StyleInclude = typeSystem.GetType("Avalonia.Markup.Xaml.Styling.StyleInclude");
            ResourceInclude = typeSystem.GetType("Avalonia.Markup.Xaml.Styling.ResourceInclude");
            MergeResourceInclude = typeSystem.GetType("Avalonia.Markup.Xaml.Styling.MergeResourceInclude");
            IResourceDictionary = typeSystem.GetType("Avalonia.Controls.IResourceDictionary");
            ResourceDictionary = typeSystem.GetType("Avalonia.Controls.ResourceDictionary");
            ResourceDictionaryDeferredAdd = ResourceDictionary.GetMethod("AddDeferred", XamlIlTypes.Void, true, XamlIlTypes.Object,
                typeSystem.GetType("Avalonia.Controls.IDeferredContent"));
            ResourceDictionaryNotSharedDeferredAdd = ResourceDictionary.GetMethod("AddNotSharedDeferred", XamlIlTypes.Void, true, XamlIlTypes.Object,
                typeSystem.GetType("Avalonia.Controls.IDeferredContent"));

            ResourceDictionaryEnsureCapacity = ResourceDictionary.GetMethod("EnsureCapacity", XamlIlTypes.Void, true, XamlIlTypes.Int32);
            ResourceDictionaryGetCount = ResourceDictionary.GetMethod("get_Count", XamlIlTypes.Int32, true);
            IThemeVariantProvider = typeSystem.GetType("Avalonia.Controls.IThemeVariantProvider");
            UriKind = typeSystem.GetType("System.UriKind");
            UriConstructor = Uri.GetConstructor(new List<IXamlType>() { typeSystem.WellKnownTypes.String, UriKind });
            Style = typeSystem.GetType("Avalonia.Styling.Style");
            Container = typeSystem.GetType("Avalonia.Styling.ContainerQuery");
            Styles = typeSystem.GetType("Avalonia.Styling.Styles");
            StyleQueries = typeSystem.GetType("Avalonia.Styling.StyleQueries");
            Selectors = typeSystem.GetType("Avalonia.Styling.Selectors");
            ControlTheme = typeSystem.GetType("Avalonia.Styling.ControlTheme");
            ControlTemplate = typeSystem.GetType("Avalonia.Markup.Xaml.Templates.ControlTemplate");
            IReadOnlyListOfT = typeSystem.GetType("System.Collections.Generic.IReadOnlyList`1");
            EventHandlerT = typeSystem.GetType("System.EventHandler`1");
            Interactivity = new InteractivityWellKnownTypes(typeSystem, typeSystem.WellKnownTypes);

            GetClassProperty = typeSystem.GetType("Avalonia.StyledElementExtensions")
                .GetMethod(name: "GetClassProperty",
                returnType: AvaloniaProperty,
                allowDowncast:false,
                typeSystem.WellKnownTypes.String
                );

            var xamlSourceInfo = typeSystem.GetType("Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo");
            XamlSourceInfoConstructor = xamlSourceInfo.GetConstructor([
                XamlIlTypes.Int32, XamlIlTypes.Int32, XamlIlTypes.String
            ]);
            XamlSourceInfoSetter =
                xamlSourceInfo.GetMethod("SetXamlSourceInfo", XamlIlTypes.Void, false, XamlIlTypes.Object, xamlSourceInfo);
            XamlSourceInfoDictionarySetter =
                xamlSourceInfo.GetMethod("SetXamlSourceInfo", XamlIlTypes.Void, false, IResourceDictionary, XamlIlTypes.Object, xamlSourceInfo);
        }
    }

    static class AvaloniaXamlIlWellKnownTypesExtensions
    {
        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this TransformerConfiguration cfg)
            => cfg.GetExtra<AvaloniaXamlIlWellKnownTypes>();

        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this AstTransformationContext ctx)
            => ctx.Configuration.GetAvaloniaTypes();

        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> ctx)
            => ctx.Configuration.GetAvaloniaTypes();

        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this AstGroupTransformationContext ctx)
            => ctx.Configuration.GetAvaloniaTypes();
    }
}
