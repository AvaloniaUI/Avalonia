using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class AvaloniaXamlIlCompilerConfiguration : TransformerConfiguration
    {
        public XamlIlClrPropertyInfoEmitter ClrPropertyEmitter { get; }
        public XamlIlPropertyInfoAccessorFactoryEmitter AccessorFactoryEmitter { get; }

        public AvaloniaXamlIlCompilerConfiguration(IXamlTypeSystem typeSystem, 
            IXamlAssembly defaultAssembly, 
            XamlLanguageTypeMappings typeMappings,
            XamlXmlnsMappings xmlnsMappings,
            XamlValueConverter customValueConverter,
            XamlIlClrPropertyInfoEmitter clrPropertyEmitter,
            XamlIlPropertyInfoAccessorFactoryEmitter accessorFactoryEmitter)
            : base(typeSystem, defaultAssembly, typeMappings, xmlnsMappings, customValueConverter)
        {
            ClrPropertyEmitter = clrPropertyEmitter;
            AccessorFactoryEmitter = accessorFactoryEmitter;
            AddExtra(ClrPropertyEmitter);
            AddExtra(AccessorFactoryEmitter);
        }
    }
}
