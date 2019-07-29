using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class AvaloniaXamlIlCompilerConfiguration : XamlIlTransformerConfiguration
    {
        public XamlIlClrPropertyInfoEmitter ClrPropertyEmitter { get; }
        public XamlIlPropertyInfoAccessorFactoryEmitter AccessorFactoryEmitter { get; }

        public AvaloniaXamlIlCompilerConfiguration(IXamlIlTypeSystem typeSystem, 
            IXamlIlAssembly defaultAssembly, 
            XamlIlLanguageTypeMappings typeMappings,
            XamlIlXmlnsMappings xmlnsMappings,
            XamlIlValueConverter customValueConverter,
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
