using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Generators.Common.Domain;
using XamlX.Ast;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Compiler;

internal sealed class MiniCompiler : XamlCompiler<object, IXamlEmitResult>
{
    public const string AvaloniaXmlnsDefinitionAttribute = "Avalonia.Metadata.XmlnsDefinitionAttribute";

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.Roslyn)]
    public static MiniCompiler CreateNoop()
    {
        var typeSystem = new NoopTypeSystem();
        var mappings = new XamlLanguageTypeMappings(typeSystem);
        var diagnosticsHandler = new XamlDiagnosticsHandler();

        var configuration = new TransformerConfiguration(
            typeSystem,
            typeSystem.Assemblies.First(),
            mappings,
            diagnosticsHandler: diagnosticsHandler);
        return new MiniCompiler(configuration);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.Roslyn)]
    public static MiniCompiler CreateRoslyn(RoslynTypeSystem typeSystem, params string[] additionalTypes)
    {
        var mappings = new XamlLanguageTypeMappings(typeSystem);
        foreach (var additionalType in additionalTypes)
            mappings.XmlnsAttributes.Add(typeSystem.GetType(additionalType));

        var diagnosticsHandler = new XamlDiagnosticsHandler();

        var configuration = new TransformerConfiguration(
            typeSystem,
            typeSystem.Assemblies.First(),
            mappings,
            diagnosticsHandler: diagnosticsHandler);
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
    }

    public IXamlType ResolveXamlType(XamlXmlType type)
    {
        var clrTypeRef = TypeReferenceResolver.ResolveType(
            new AstTransformationContext(_configuration, null), ToTypeRef(type));
        return clrTypeRef.Type;

        static XamlAstXmlTypeReference ToTypeRef(XamlXmlType type) => new(EmptyLineInfo.Instance,
            type.XmlNamespace, type.Name, type.GenericArguments.Select(ToTypeRef));
    }
    
    protected override XamlEmitContext<object, IXamlEmitResult> InitCodeGen(
        IFileSource file,
        IXamlTypeBuilder<object> declaringType,
        object codeGen,
        XamlRuntimeContext<object, IXamlEmitResult> context,
        bool needContextLocal) =>
        throw new NotSupportedException();

    private class EmptyLineInfo : IXamlLineInfo
    {
        public static IXamlLineInfo Instance { get; } = new EmptyLineInfo();
        public int Line { get => 0; set { } }
        public int Position { get => 0; set { } }
    }
}
