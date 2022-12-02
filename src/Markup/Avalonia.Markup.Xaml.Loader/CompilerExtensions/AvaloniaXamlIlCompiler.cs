using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class AvaloniaXamlIlCompiler : XamlILCompiler
    {
        private readonly IXamlType _contextType;
        private readonly AvaloniaXamlIlDesignPropertiesTransformer _designTransformer;
        private readonly AvaloniaBindingExtensionTransformer _bindingTransformer;

        private AvaloniaXamlIlCompiler(TransformerConfiguration configuration, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings)
            : base(configuration, emitMappings, true)
        {
            void InsertAfter<T>(params IXamlAstTransformer[] t)
                => Transformers.InsertRange(Transformers.FindIndex(x => x is T) + 1, t);

            void InsertBefore<T>(params IXamlAstTransformer[] t)
                => Transformers.InsertRange(Transformers.FindIndex(x => x is T), t);


            // Before everything else

            Transformers.Insert(0, new XNameTransformer());
            Transformers.Insert(1, new IgnoredDirectivesTransformer());
            Transformers.Insert(2, _designTransformer = new AvaloniaXamlIlDesignPropertiesTransformer());
            Transformers.Insert(3, _bindingTransformer = new AvaloniaBindingExtensionTransformer());

            // Targeted
            InsertBefore<PropertyReferenceResolver>(
                new AvaloniaXamlIlResolveClassesPropertiesTransformer(),
                new AvaloniaXamlIlTransformInstanceAttachedProperties(),
                new AvaloniaXamlIlTransformSyntheticCompiledBindingMembers());
            InsertAfter<PropertyReferenceResolver>(
                new AvaloniaXamlIlAvaloniaPropertyResolver(),
                new AvaloniaXamlIlReorderClassesPropertiesTransformer()
            );

            InsertBefore<ContentConvertTransformer>(
                new AvaloniaXamlIlControlThemeTransformer(),
                new AvaloniaXamlIlSelectorTransformer(),
                new AvaloniaXamlIlControlTemplateTargetTypeMetadataTransformer(),
                new AvaloniaXamlIlBindingPathParser(),
                new AvaloniaXamlIlPropertyPathTransformer(),
                new AvaloniaXamlIlSetterTargetTypeMetadataTransformer(),
                new AvaloniaXamlIlSetterTransformer(),
                new AvaloniaXamlIlConstructorServiceProviderTransformer(),
                new AvaloniaXamlIlTransitionsTypeMetadataTransformer(),
                new AvaloniaXamlIlResolveByNameMarkupExtensionReplacer()
            );
            InsertBefore<ConvertPropertyValuesToAssignmentsTransformer>(
                new AvaloniaXamlIlOptionMarkupExtensionTransformer());

            InsertAfter<TypeReferenceResolver>(
                new XDataTypeTransformer());

            InsertBefore<DeferredContentTransformer>(
                new AvaloniaXamlIlDeferredResourceTransformer()
            );

            // After everything else
            InsertBefore<NewObjectTransformer>(
                new AddNameScopeRegistration(),
                new AvaloniaXamlIlDataContextTypeTransformer(),
                new AvaloniaXamlIlBindingPathTransformer(),
                new AvaloniaXamlIlCompiledBindingsMetadataRemover()
                );

            Transformers.Add(new AvaloniaXamlIlControlTemplatePriorityTransformer());
            Transformers.Add(new AvaloniaXamlIlMetadataRemover());
            Transformers.Add(new AvaloniaXamlIlRootObjectScope());

            Emitters.Add(new AvaloniaNameScopeRegistrationXamlIlNodeEmitter());
            Emitters.Add(new AvaloniaXamlIlRootObjectScope.Emitter());
            
            GroupTransformers = new()
            {
                new AvaloniaXamlIncludeTransformer()
            };
        }
        public AvaloniaXamlIlCompiler(TransformerConfiguration configuration,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
            IXamlTypeBuilder<IXamlILEmitter> contextTypeBuilder)
            : this(configuration, emitMappings)
        {
            _contextType = CreateContextType(contextTypeBuilder);
        }


        public AvaloniaXamlIlCompiler(TransformerConfiguration configuration,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
            IXamlType contextType) : this(configuration, emitMappings)
        {
            _contextType = contextType;
        }

        public const string PopulateName = "__AvaloniaXamlIlPopulate";
        public const string BuildName = "__AvaloniaXamlIlBuild";

        public bool IsDesignMode
        {
            get => _designTransformer.IsDesignMode;
            set => _designTransformer.IsDesignMode = value;
        }

        public bool DefaultCompileBindings
        {
            get => _bindingTransformer.CompileBindingsByDefault;
            set => _bindingTransformer.CompileBindingsByDefault = value;
        }

        public List<IXamlAstGroupTransformer> GroupTransformers { get; }

        public void TransformGroup(IReadOnlyCollection<IXamlDocumentResource> documents, bool strict = true)
        {
            var ctx = new AstGroupTransformationContext(documents, _configuration, strict);
            foreach (var transformer in GroupTransformers)
            {
                foreach (var doc in documents)
                {
                    var root = doc.XamlDocument.Root;
                    ctx.CurrentDocument = doc;
                    ctx.RootObject = (IXamlAstValueNode)root;
                    ctx.VisitChildren(ctx.RootObject, transformer);
                    root = ctx.Visit(root, transformer);

                    doc.XamlDocument.Root = root;
                }
            }
        }

        public XamlDocument Parse(string xaml, IXamlType overrideRootType)
        {
            var parsed = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });

            var rootObject = (XamlAstObjectNode)parsed.Root;

            var classDirective = rootObject.Children
                .OfType<XamlAstXmlDirective>().FirstOrDefault(x =>
                    x.Namespace == XamlNamespaces.Xaml2006
                    && x.Name == "Class");

            var rootType =
                classDirective != null ?
                    new XamlAstClrTypeReference(classDirective,
                        _configuration.TypeSystem.GetType(((XamlAstTextNode)classDirective.Values[0]).Text),
                        false) :
                    TypeReferenceResolver.ResolveType(CreateTransformationContext(parsed, true),
                        (XamlAstXmlTypeReference)rootObject.Type, true);


            if (overrideRootType != null)
            {
                if (!rootType.Type.IsAssignableFrom(overrideRootType))
                    throw new XamlX.XamlLoadException(
                        $"Unable to substitute {rootType.Type.GetFqn()} with {overrideRootType.GetFqn()}", rootObject);
                rootType = new XamlAstClrTypeReference(rootObject, overrideRootType, false);
            }

            OverrideRootType(parsed, rootType);

            return parsed;
        }

        public void Compile(XamlDocument document, IXamlTypeBuilder<IXamlILEmitter> tb, IXamlMethodBuilder<IXamlILEmitter> populateMethod, IXamlMethodBuilder<IXamlILEmitter> buildMethod, string baseUri, IFileSource fileSource)
        {
            Compile(document, _contextType, populateMethod, buildMethod,
                _configuration.TypeMappings.XmlNamespaceInfoProvider == null ?
                    null :
                    tb.DefineSubType(_configuration.WellKnownTypes.Object,
                        "__AvaloniaXamlIlNsInfo", false), (name, bt) => tb.DefineSubType(bt, name, false),
                (s, returnType, parameters) => tb.DefineDelegateSubType(s, false, returnType, parameters), baseUri,
                fileSource);
        }
        
        public void ParseAndCompile(string xaml, string baseUri, IFileSource fileSource, IXamlTypeBuilder<IXamlILEmitter> tb, IXamlType overrideRootType)
        {
            var parsed = Parse(xaml, overrideRootType);

            Transform(parsed);
            Compile(parsed, tb, _contextType, PopulateName, BuildName, "__AvaloniaXamlIlNsInfo", baseUri, fileSource);
        }

        public void OverrideRootType(XamlDocument doc, IXamlAstTypeReference newType)
        {
            var root = (XamlAstObjectNode)doc.Root;
            var oldType = root.Type;
            if (oldType.Equals(newType))
                return;

            root.Type = newType;
            foreach (var child in root.Children.OfType<XamlAstXamlPropertyValueNode>())
            {
                if (child.Property is XamlAstNamePropertyReference prop)
                {
                    if (prop.DeclaringType.Equals(oldType))
                        prop.DeclaringType = newType;
                    if (prop.TargetType.Equals(oldType))
                        prop.TargetType = newType;
                }
            }
        }
    }
}
