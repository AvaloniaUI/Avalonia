using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    /*
        This file is used in the build task.
        ONLY use types from netstandard and XamlIl. NO dependencies on Avalonia are allowed. Only strings.
        No, nameof isn't welcome here either
     */
    
    class AvaloniaXamlIlLanguage
    {
        public static (XamlLanguageTypeMappings language, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emit) Configure(IXamlTypeSystem typeSystem)
        {
            var runtimeHelpers = typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");
            var assignBindingAttribute = typeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            var bindingType = typeSystem.GetType("Avalonia.Data.IBinding");
            var rv = new XamlLanguageTypeMappings(typeSystem)
            {
                SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize"),
                XmlnsAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.XmlnsDefinitionAttribute"),
                },
                ContentAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.ContentAttribute")
                },
                ProvideValueTarget = typeSystem.GetType("Avalonia.Markup.Xaml.IProvideValueTarget"),
                RootObjectProvider = typeSystem.GetType("Avalonia.Markup.Xaml.IRootObjectProvider"),
                RootObjectProviderIntermediateRootPropertyName = "IntermediateRootObject",
                UriContextProvider = typeSystem.GetType("Avalonia.Markup.Xaml.IUriContext"),
                ParentStackProvider =
                    typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlParentStackProvider"),

                XmlNamespaceInfoProvider =
                    typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlXmlNamespaceInfoProvider"),
                DeferredContentPropertyAttributes = {typeSystem.GetType("Avalonia.Metadata.TemplateContentAttribute")},
                DeferredContentExecutorCustomization =
                    runtimeHelpers.FindMethod(m => m.Name == "DeferredTransformationFactoryV1"),
                UsableDuringInitializationAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.UsableDuringInitializationAttribute"),
                },
                InnerServiceProviderFactoryMethod =
                    runtimeHelpers.FindMethod(m => m.Name == "CreateInnerServiceProviderV1"),
            };
            rv.CustomAttributeResolver = new AttributeResolver(typeSystem, rv);

            var emit = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>
            {
                ProvideValueTargetPropertyEmitter = XamlIlAvaloniaPropertyHelper.EmitProvideValueTarget,
                ContextTypeBuilderCallback = (b, c) => EmitNameScopeField(rv, typeSystem, b, c)
            };
            return (rv, emit);
        }

        public const string ContextNameScopeFieldName = "AvaloniaNameScope";

        private static void EmitNameScopeField(XamlLanguageTypeMappings mappings,
            IXamlTypeSystem typeSystem,
            IXamlTypeBuilder<IXamlILEmitter> typebuilder, IXamlILEmitter constructor)
        {

            var nameScopeType = typeSystem.FindType("Avalonia.Controls.INameScope");
            var field = typebuilder.DefineField(nameScopeType, 
                ContextNameScopeFieldName, true, false);
            constructor
                .Ldarg_0()
                .Ldarg(1)
                .Ldtype(nameScopeType)
                .EmitCall(mappings.ServiceProvider.GetMethod(new FindMethodMethodSignature("GetService",
                    typeSystem.FindType("System.Object"), typeSystem.FindType("System.Type"))))
                .Stfld(field);
        }
        

        class AttributeResolver : IXamlCustomAttributeResolver
        {
            private readonly IXamlType _typeConverterAttribute;

            private readonly List<KeyValuePair<IXamlType, IXamlType>> _converters =
                new List<KeyValuePair<IXamlType, IXamlType>>();

            private readonly IXamlType _avaloniaList;
            private readonly IXamlType _avaloniaListConverter;


            public AttributeResolver(IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings)
            {
                _typeConverterAttribute = mappings.TypeConverterAttributes.First();

                void AddType(IXamlType type, IXamlType conv) 
                    => _converters.Add(new KeyValuePair<IXamlType, IXamlType>(type, conv));

                void Add(string type, string conv)
                    => AddType(typeSystem.GetType(type), typeSystem.GetType(conv));
                
                Add("Avalonia.Media.IImage","Avalonia.Markup.Xaml.Converters.BitmapTypeConverter");
                Add("Avalonia.Media.Imaging.IBitmap","Avalonia.Markup.Xaml.Converters.BitmapTypeConverter");
                var ilist = typeSystem.GetType("System.Collections.Generic.IList`1");
                AddType(ilist.MakeGenericType(typeSystem.GetType("Avalonia.Point")),
                    typeSystem.GetType("Avalonia.Markup.Xaml.Converters.PointsListTypeConverter"));
                Add("Avalonia.Controls.WindowIcon","Avalonia.Markup.Xaml.Converters.IconTypeConverter");
                Add("System.Globalization.CultureInfo", "System.ComponentModel.CultureInfoConverter");
                Add("System.Uri", "Avalonia.Markup.Xaml.Converters.AvaloniaUriTypeConverter");
                Add("System.TimeSpan", "Avalonia.Markup.Xaml.Converters.TimeSpanTypeConverter");
                Add("Avalonia.Media.FontFamily","Avalonia.Markup.Xaml.Converters.FontFamilyTypeConverter");
                _avaloniaList = typeSystem.GetType("Avalonia.Collections.AvaloniaList`1");
                _avaloniaListConverter = typeSystem.GetType("Avalonia.Collections.AvaloniaListConverter`1");
            }

            IXamlType LookupConverter(IXamlType type)
            {
                foreach(var p in _converters)
                    if (p.Key.Equals(type))
                        return p.Value;
                if (type.GenericTypeDefinition?.Equals(_avaloniaList) == true)
                    return _avaloniaListConverter.MakeGenericType(type.GenericArguments[0]);
                return null;
            }

            class ConstructedAttribute : IXamlCustomAttribute
            {
                public bool Equals(IXamlCustomAttribute other) => false;
                
                public IXamlType Type { get; }
                public List<object> Parameters { get; }
                public Dictionary<string, object> Properties { get; }

                public ConstructedAttribute(IXamlType type, List<object> parameters, Dictionary<string, object> properties)
                {
                    Type = type;
                    Parameters = parameters ?? new List<object>();
                    Properties = properties ?? new Dictionary<string, object>();
                }
            }
            
            public IXamlCustomAttribute GetCustomAttribute(IXamlType type, IXamlType attributeType)
            {
                if (attributeType.Equals(_typeConverterAttribute))
                {
                    var conv = LookupConverter(type);
                    if (conv != null)
                        return new ConstructedAttribute(_typeConverterAttribute, new List<object>() {conv}, null);
                }

                return null;
            }

            public IXamlCustomAttribute GetCustomAttribute(IXamlProperty property, IXamlType attributeType)
            {
                return null;
            }
        }

        public static bool CustomValueConverter(AstTransformationContext context,
            IXamlAstValueNode node, IXamlType type, out IXamlAstValueNode result)
        {
            if (type.FullName == "System.TimeSpan" 
                && node is XamlAstTextNode tn
                && !tn.Text.Contains(":"))
            {
                var seconds = double.Parse(tn.Text, CultureInfo.InvariantCulture);
                result = new XamlStaticOrTargetedReturnMethodCallNode(tn,
                    type.FindMethod("FromSeconds", type, false, context.Configuration.WellKnownTypes.Double),
                    new[]
                    {
                        new XamlConstantNode(tn, context.Configuration.WellKnownTypes.Double, seconds)
                    });
                return true;
            }

            if (type.FullName == "Avalonia.AvaloniaProperty")
            {
                var scope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>().FirstOrDefault();
                if (scope == null)
                    throw new XamlX.XamlLoadException("Unable to find the parent scope for AvaloniaProperty lookup", node);
                if (!(node is XamlAstTextNode text))
                    throw new XamlX.XamlLoadException("Property should be a text node", node);
                result = XamlIlAvaloniaPropertyHelper.CreateNode(context, text.Text, scope.TargetType, text);
                return true;
            }

            result = null;
            return false;
        }
    }
}
