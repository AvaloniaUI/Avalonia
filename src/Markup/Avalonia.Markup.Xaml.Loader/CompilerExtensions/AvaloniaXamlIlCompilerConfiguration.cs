using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class AvaloniaXamlIlCompilerConfiguration : TransformerConfiguration
    {
        public XamlIlClrPropertyInfoEmitter ClrPropertyEmitter { get; }
        public XamlIlPropertyInfoAccessorFactoryEmitter AccessorFactoryEmitter { get; }
        public XamlIlTrampolineBuilder TrampolineBuilder { get; }

        public AvaloniaXamlIlCompilerConfiguration(IXamlTypeSystem typeSystem, 
            IXamlAssembly defaultAssembly, 
            XamlLanguageTypeMappings typeMappings,
            XamlXmlnsMappings xmlnsMappings,
            XamlValueConverter customValueConverter,
            XamlIlClrPropertyInfoEmitter clrPropertyEmitter,
            XamlIlPropertyInfoAccessorFactoryEmitter accessorFactoryEmitter,
            XamlIlTrampolineBuilder trampolineBuilder,
            IXamlIdentifierGenerator identifierGenerator = null)
            : base(typeSystem, defaultAssembly, typeMappings, xmlnsMappings, customValueConverter, identifierGenerator)
        {
            ClrPropertyEmitter = clrPropertyEmitter;
            AccessorFactoryEmitter = accessorFactoryEmitter;
            TrampolineBuilder = trampolineBuilder;
            AddExtra(ClrPropertyEmitter);
            AddExtra(AccessorFactoryEmitter);
            AddExtra(TrampolineBuilder);
        }
    }
}
