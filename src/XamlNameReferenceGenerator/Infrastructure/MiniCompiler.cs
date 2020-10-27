using System;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlNameReferenceGenerator.Infrastructure
{
    internal sealed class MiniCompiler : XamlCompiler<object, IXamlEmitResult>
    {
        public static MiniCompiler CreateDefault(RoslynTypeSystem typeSystem)
        {
            var avaloniaXmlns = typeSystem.GetType("Avalonia.Metadata.XmlnsDefinitionAttribute");
            var configuration = new TransformerConfiguration(
                typeSystem,
                typeSystem.Assemblies[0],
                new XamlLanguageTypeMappings(typeSystem) {XmlnsAttributes = {avaloniaXmlns}});
            return new MiniCompiler(configuration);
        }
        
        private MiniCompiler(TransformerConfiguration configuration)
            : base(configuration, new XamlLanguageEmitMappings<object, IXamlEmitResult>(), false)
        {
            Transformers.Add(new NameDirectiveTransformer());
            Transformers.Add(new KnownDirectivesTransformer());
            Transformers.Add(new XamlIntrinsicsTransformer());
            Transformers.Add(new XArgumentsTransformer());
            Transformers.Add(new TypeReferenceResolver());
            Transformers.Add(new PropertyReferenceResolver());
            Transformers.Add(new ResolvePropertyValueAddersTransformer());
            Transformers.Add(new ConstructableObjectTransformer());
        }

        protected override XamlEmitContext<object, IXamlEmitResult> InitCodeGen(
            IFileSource file,
            Func<string, IXamlType, IXamlTypeBuilder<object>> createSubType,
            object codeGen, XamlRuntimeContext<object, IXamlEmitResult> context,
            bool needContextLocal) =>
            throw new NotSupportedException();
    }
}