using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Designer.Metadata
{
    [Serializable]
    public class AvaloniaDesignerMetadata
    {
        public List<MetadataType> Types { get; set; }
        public List<MetadataNamespaceAlias> NamespaceAliases { get; set; }
    }


    [Serializable]
    public class MetadataType
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<MetadataProperty> Properties { get; set; }
        public string FullName => Namespace + "." + Name;
    }

    [Serializable]
    public class MetadataNamespaceAlias
    {
        public string Namespace { get; set; }
        public string XmlNamespace { get; set; }
    }

    [Serializable]
    public class MetadataProperty
    {
        public string Name { get; set; }
        public MetadataPropertyType Type { get; set; }
        public string MetadataFullTypeName { get; set; }
        public string[] EnumValues { get; set; }
    }



    [Serializable]
    public enum MetadataPropertyType
    {
        BasicType,
        MetadataType,
        Enum
    }
}
