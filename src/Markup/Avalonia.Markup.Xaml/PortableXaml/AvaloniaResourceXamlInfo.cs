using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    [DataContract]
    class AvaloniaResourceXamlInfo
    {
        [DataMember]
        public Dictionary<string, string> ClassToResourcePathIndex { get; set; } = new Dictionary<string, string>();
    }
}
