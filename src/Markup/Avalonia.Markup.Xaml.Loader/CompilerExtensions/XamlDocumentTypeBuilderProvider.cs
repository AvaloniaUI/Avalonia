#nullable enable

using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal sealed class XamlDocumentTypeBuilderProvider
{
    public XamlDocumentTypeBuilderProvider(
        IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
        IXamlMethodBuilder<IXamlILEmitter> populateMethod,
        IXamlMethodBuilder<IXamlILEmitter>? buildMethod)
    {
        TypeBuilder = typeBuilder;
        PopulateMethod = populateMethod;
        BuildMethod = buildMethod;
    }

    public IXamlTypeBuilder<IXamlILEmitter> TypeBuilder { get; }
    public IXamlMethodBuilder<IXamlILEmitter> PopulateMethod { get; }
    public IXamlMethodBuilder<IXamlILEmitter>? BuildMethod { get; }
}
