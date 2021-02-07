using System;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.NameGenerator.Compiler
{
    internal sealed class MiniCompiler : XamlCompiler<object, IXamlEmitResult>
    {
        public static MiniCompiler CreateDefault(RoslynTypeSystem typeSystem, params string[] additionalTypes)
        {
            var mappings = new XamlLanguageTypeMappings(typeSystem);
            foreach (var additionalType in additionalTypes)
                mappings.XmlnsAttributes.Add(typeSystem.GetType(additionalType));

            var configuration = new TransformerConfiguration(
                typeSystem,
                typeSystem.Assemblies[0],
                mappings);
            return new MiniCompiler(configuration);
        }
        
        private MiniCompiler(TransformerConfiguration configuration)
            : base(configuration, new XamlLanguageEmitMappings<object, IXamlEmitResult>(), false)
        {
            Transformers.Add(new NameDirectiveTransformer());
            Transformers.Add(new DataTemplateTransformer());
            Transformers.Add(new KnownDirectivesTransformer());
            Transformers.Add(new XamlIntrinsicsTransformer());
            Transformers.Add(new XArgumentsTransformer());
            Transformers.Add(new TypeReferenceResolver());
        }

        protected override XamlEmitContext<object, IXamlEmitResult> InitCodeGen(
            IFileSource file,
            Func<string, IXamlType, IXamlTypeBuilder<object>> createSubType,
            object codeGen, XamlRuntimeContext<object, IXamlEmitResult> context,
            bool needContextLocal) =>
            throw new NotSupportedException();
    }
}