#nullable enable

using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal sealed class XamlDocumentTypeBuilderProvider
{
    public XamlDocumentTypeBuilderProvider(
        IXamlTypeBuilder<IXamlILEmitter> populateDeclaringType,
        IXamlMethodBuilder<IXamlILEmitter> populateMethod,
        IXamlTypeBuilder<IXamlILEmitter>? buildDeclaringType,
        IXamlMethodBuilder<IXamlILEmitter>? buildMethod)
    {
        PopulateDeclaringType = populateDeclaringType;
        PopulateMethod = populateMethod;
        BuildDeclaringType = buildDeclaringType;
        BuildMethod = buildMethod;
    }

    public IXamlTypeBuilder<IXamlILEmitter> PopulateDeclaringType { get; }
    public IXamlMethodBuilder<IXamlILEmitter> PopulateMethod { get; }
    public IXamlTypeBuilder<IXamlILEmitter>? BuildDeclaringType { get; }
    public IXamlMethodBuilder<IXamlILEmitter>? BuildMethod { get; }
}
