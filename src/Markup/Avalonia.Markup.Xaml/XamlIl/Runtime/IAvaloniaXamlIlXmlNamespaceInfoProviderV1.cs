using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime
{
    public interface IAvaloniaXamlIlXmlNamespaceInfoProvider
    {
        IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces { get; }
    }
    
    public class AvaloniaXamlIlXmlNamespaceInfo
    {
        public string ClrNamespace { get; set; }
        public string ClrAssemblyName { get; set; }
    }
}
