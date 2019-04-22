using System.Collections.Generic;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Parsers;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    public class AvaloniaXamlIlCompiler : XamlIlCompiler
    {
        private readonly IXamlIlType _contextType;
        private readonly AvaloniaXamlIlDesignPropertiesTransformer _designTransformer;

        private AvaloniaXamlIlCompiler(XamlIlTransformerConfiguration configuration) : base(configuration, true)
        {
            // Before everything else
            
            Transformers.Insert(0, new XNameTransformer());
            Transformers.Insert(1, new IgnoredDirectivesTransformer());
            Transformers.Insert(2, _designTransformer = new AvaloniaXamlIlDesignPropertiesTransformer());
            
            
            // Targeted

            Transformers.Insert(Transformers.FindIndex(x => x is XamlIlPropertyReferenceResolver),
                new AvaloniaXamlIlTransformInstanceAttachedProperties());
            
            Transformers.Insert(Transformers.FindIndex(x => x is XamlIlXamlPropertyValueTransformer),
                new KnownPseudoMarkupExtensionsTransformer());

            Transformers.InsertRange(Transformers.FindIndex(x => x is XamlIlNewObjectTransformer),
                new IXamlIlAstTransformer[]
                {
                    new AvaloniaXamlIlSelectorTransformer(),
                    new AvaloniaXamlIlSetterTransformer(),
                    new AvaloniaXamlIlControlTemplateTargetTypeMetadataTransformer(),
                    new AvaloniaXamlIlConstructorServiceProviderTransformer()
                }
            );
            // After everything else
            
            Transformers.Add(new AddNameScopeRegistration());
            Transformers.Add(new AvaloniaXamlIlMetadataRemover());

        }

        public AvaloniaXamlIlCompiler(XamlIlTransformerConfiguration configuration,
            IXamlIlTypeBuilder contextTypeBuilder) : this(configuration)
        {
            _contextType = CreateContextType(contextTypeBuilder);
        }

        
        public AvaloniaXamlIlCompiler(XamlIlTransformerConfiguration configuration,
            IXamlIlType contextType) : this(configuration)
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

        public void ParseAndCompile(string xaml, string baseUri, IFileSource fileSource, IXamlIlTypeBuilder tb, IXamlIlType overrideRootType)
        {
            var parsed = XDocumentXamlIlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });
            
            if (overrideRootType != null)
            {
                var rootObject = (XamlIlAstObjectNode)parsed.Root;

                var originalType = XamlIlTypeReferenceResolver.ResolveType(CreateTransformationContext(parsed, true),
                    (XamlIlAstXmlTypeReference)rootObject.Type, true);

                if (!originalType.Type.IsAssignableFrom(overrideRootType))
                    throw new XamlIlLoadException(
                        $"Unable to substitute {originalType.Type.GetFqn()} with {overrideRootType.GetFqn()}", rootObject);
                rootObject.Type = new XamlIlAstClrTypeReference(rootObject, overrideRootType, false);
            }

            Transform(parsed);
            Compile(parsed, tb, _contextType, PopulateName, BuildName, "__AvaloniaXamlIlNsInfo", baseUri, fileSource);
            
        }
        
        
    }
}
