using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    /*
        This file is used in the build task.
        ONLY use types from netstandard and XamlIl. NO dependencies on Avalonia are allowed. Only strings.
        No, nameof isn't welcome here either
     */
    
    class AvaloniaXamlIlLanguage
    {
        public static XamlIlLanguageTypeMappings Configure(IXamlIlTypeSystem typeSystem)
        {
            var runtimeHelpers = typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");
            var assignBindingAttribute = typeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            var bindingType = typeSystem.GetType("Avalonia.Data.IBinding");
            var rv = new XamlIlLanguageTypeMappings(typeSystem)
            {
                SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize"),
                XmlnsAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.XmlnsDefinitionAttribute"),
                    typeSystem.FindType("Portable.Xaml.Markup.XmlnsDefinitionAttribute")
                },
                ContentAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.ContentAttribute")
                },
                ProvideValueTarget = typeSystem.GetType("Portable.Xaml.Markup.IProvideValueTarget"),
                RootObjectProvider = typeSystem.GetType("Portable.Xaml.IRootObjectProvider"),
                UriContextProvider = typeSystem.GetType("Portable.Xaml.Markup.IUriContext"),
                ParentStackProvider =
                    typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlParentStackProvider"),

                XmlNamespaceInfoProvider =
                    typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlXmlNamespaceInfoProvider"),
                DeferredContentPropertyAttributes = {typeSystem.GetType("Avalonia.Metadata.TemplateContentAttribute")},
                DeferredContentExecutorCustomization =
                    runtimeHelpers.FindMethod(m => m.Name == "DeferredTransformationFactoryV1"),
                UsableDuringInitializationAttributes =
                {
                    typeSystem.GetType("Portable.Xaml.Markup.UsableDuringInitializationAttribute"),
                    typeSystem.GetType("Avalonia.Metadata.UsableDuringInitializationAttribute"),
                },
                MarkupExtensionCustomResultTypes =
                {
                    bindingType,
                    typeSystem.GetType("Avalonia.UnsetValueType")
                },
                MarkupExtensionCustomResultHandler =
                    runtimeHelpers.FindMethod(m => m.Name == "ApplyNonMatchingMarkupExtensionV1"),
                InnerServiceProviderFactoryMethod =
                    runtimeHelpers.FindMethod(m => m.Name == "CreateInnerServiceProviderV1"),
                ShouldIgnoreMarkupExtensionCustomResultForProperty = (prop, customType) =>
                {
                    if (prop.Name == "Value" && prop.Setter?.DeclaringType.FullName == "Avalonia.Styling.Setter")
                        return true;
                    if (customType.Equals(bindingType) &&
                        prop.CustomAttributes.Any(a => a.Type.Equals(assignBindingAttribute)))
                        return true;
                    return false;
                },
                ProvideValueTargetPropertyEmitter = XamlIlAvaloniaPropertyHelper.Emit,
            };
            rv.CustomAttributeResolver = new AttributeResolver(typeSystem, rv);
            return rv;
        }

        class AttributeResolver : IXamlIlCustomAttributeResolver
        {
            private readonly IXamlIlType _typeConverterAttribute;

            private readonly List<KeyValuePair<IXamlIlType, IXamlIlType>> _converters =
                new List<KeyValuePair<IXamlIlType, IXamlIlType>>();

            private readonly IXamlIlType _avaloniaList;
            private readonly IXamlIlType _avaloniaListConverter;


            public AttributeResolver(IXamlIlTypeSystem typeSystem, XamlIlLanguageTypeMappings mappings)
            {
                _typeConverterAttribute = mappings.TypeConverterAttributes.First();

                void AddType(IXamlIlType type, IXamlIlType conv) 
                    => _converters.Add(new KeyValuePair<IXamlIlType, IXamlIlType>(type, conv));

                void Add(string type, string conv)
                    => AddType(typeSystem.GetType(type), typeSystem.GetType(conv));
                
                
                //Add("Avalonia.AvaloniaProperty","Avalonia.Markup.Xaml.Converters.AvaloniaPropertyTypeConverter");
                Add("Avalonia.Media.Imaging.IBitmap","Avalonia.Markup.Xaml.Converters.BitmapTypeConverter");
                var ilist = typeSystem.GetType("System.Collections.Generic.IList`1");
                AddType(ilist.MakeGenericType(typeSystem.GetType("Avalonia.Point")),
                    typeSystem.GetType("Avalonia.Markup.Xaml.Converters.PointsListTypeConverter"));
                Add("Avalonia.Controls.Templates.IMemberSelector",
                    "Avalonia.Markup.Xaml.Converters.MemberSelectorTypeConverter");
                Add("Avalonia.Styling.Selector","Avalonia.Markup.Xaml.Converters.SelectorTypeConverter");
                Add("Avalonia.Controls.WindowIcon","Avalonia.Markup.Xaml.Converters.IconTypeConverter");
                Add("System.Globalization.CultureInfo", "System.ComponentModel.CultureInfoConverter");
                Add("System.Uri", "Avalonia.Markup.Xaml.Converters.AvaloniaUriTypeConverter");
                Add("System.TimeSpan", "Avalonia.Markup.Xaml.Converters.TimeSpanTypeConverter");
                Add("Avalonia.Media.FontFamily","Avalonia.Markup.Xaml.Converters.FontFamilyTypeConverter");
                _avaloniaList = typeSystem.GetType("Avalonia.Collections.AvaloniaList`1");
                _avaloniaListConverter = typeSystem.GetType("Avalonia.Collections.AvaloniaListConverter`1");
            }

            IXamlIlType LookupConverter(IXamlIlType type)
            {
                foreach(var p in _converters)
                    if (p.Key.Equals(type))
                        return p.Value;
                if (type.GenericTypeDefinition?.Equals(_avaloniaList) == true)
                    return _avaloniaListConverter.MakeGenericType(type.GenericArguments[0]);
                return null;
            }

            class ConstructedAttribute : IXamlIlCustomAttribute
            {
                public bool Equals(IXamlIlCustomAttribute other) => false;
                
                public IXamlIlType Type { get; }
                public List<object> Parameters { get; }
                public Dictionary<string, object> Properties { get; }

                public ConstructedAttribute(IXamlIlType type, List<object> parameters, Dictionary<string, object> properties)
                {
                    Type = type;
                    Parameters = parameters ?? new List<object>();
                    Properties = properties ?? new Dictionary<string, object>();
                }
            }
            
            public IXamlIlCustomAttribute GetCustomAttribute(IXamlIlType type, IXamlIlType attributeType)
            {
                if (attributeType.Equals(_typeConverterAttribute))
                {
                    var conv = LookupConverter(type);
                    if (conv != null)
                        return new ConstructedAttribute(_typeConverterAttribute, new List<object>() {conv}, null);
                }

                return null;
            }

            public IXamlIlCustomAttribute GetCustomAttribute(IXamlIlProperty property, IXamlIlType attributeType)
            {
                return null;
            }
        }

        public static bool CustomValueConverter(XamlIlAstTransformationContext context,
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

            if (type.FullName == "Avalonia.AvaloniaProperty")
            {
                var scope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>().FirstOrDefault();
                if (scope == null)
                    throw new XamlIlLoadException("Unable to find the parent scope for AvaloniaProperty lookup", node);
                if (!(node is XamlIlAstTextNode text))
                    throw new XamlIlLoadException("Property should be a text node", node);
                result = XamlIlAvaloniaPropertyHelper.CreateNode(context, text.Text, scope.TargetType, text);
                return true;
            }

            result = null;
            return false;
        }
    }
}
