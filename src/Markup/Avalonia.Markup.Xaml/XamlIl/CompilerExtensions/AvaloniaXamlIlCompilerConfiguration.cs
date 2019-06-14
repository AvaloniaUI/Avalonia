using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class AvaloniaXamlIlCompilerConfiguration : XamlIlTransformerConfiguration
    {
        public XamlIlClrPropertyInfoEmitter ClrPropertyEmitter { get; }

        public AvaloniaXamlIlCompilerConfiguration(IXamlIlTypeSystem typeSystem, 
            IXamlIlAssembly defaultAssembly, 
            XamlIlLanguageTypeMappings typeMappings,
            XamlIlXmlnsMappings xmlnsMappings,
            XamlIlValueConverter customValueConverter,
            XamlIlClrPropertyInfoEmitter clrPropertyEmitter) : base(typeSystem, defaultAssembly, typeMappings, xmlnsMappings, customValueConverter)
        {
            ClrPropertyEmitter = clrPropertyEmitter;
            AddExtra(ClrPropertyEmitter);
        }
    }
}
