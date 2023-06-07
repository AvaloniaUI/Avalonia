#nullable disable
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Avalonia.SourceGenerator.CompositionGenerator
{
    [XmlRoot("NComposition")]
    public class GConfig
    {
        [XmlElement("Using")]
        public List<GUsing> Usings { get; set; } = new List<GUsing>();
        
        [XmlElement(typeof(GManualClass), ElementName = "Manual")]
        public List<GManualClass> ManualClasses { get; set; } = new List<GManualClass>();
        
        [XmlElement(typeof(GClass), ElementName = "Object")]
        [XmlElement(typeof(GBrush), ElementName = "Brush")]
        [XmlElement(typeof(GList), ElementName = "List")]
        public List<GClass> Classes { get; set; } = new List<GClass>();

        [XmlElement(typeof(GAnimationType), ElementName = "KeyFrameAnimation")]
        public List<GAnimationType> KeyFrameAnimations { get; set; } = new List<GAnimationType>();
    }
        
    public class GUsing
    {
        [XmlText]
        public string Name { get; set; }
    }

    public class GManualClass
    {
        [XmlAttribute]
        public string Name { get; set; }
        
        
        [XmlAttribute]
        public bool Passthrough { get; set; }
        
        [XmlAttribute]
        public string ServerName { get; set; }
    }
    
    public class GImplements
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string ServerName { get; set; }
    }
    
    public class GClass
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Inherits { get; set; }
        
        [XmlAttribute]
        public string ChangesBase { get; set; }
        
        [XmlAttribute]
        public string ServerBase { get; set; }
        
        [XmlAttribute]
        public bool CustomCtor { get; set; }
        
        [XmlAttribute]
        public bool CustomServerCtor { get; set; }
        
        [XmlAttribute]
        public bool Internal { get; set; }
        
        [XmlAttribute]
        public bool ServerOnly { get; set; }
        
        [XmlElement(typeof(GImplements), ElementName = "Implements")]
        public List<GImplements> Implements { get; set; } = new List<GImplements>();
        
        [XmlAttribute]
        public bool Abstract { get; set; }
        
        [XmlElement(typeof(GProperty), ElementName = "Property")]
        public List<GProperty> Properties { get; set; } = new List<GProperty>();
    }

    public class GBrush : GClass
    {
        [XmlAttribute]
        public bool CustomUpdate { get; set; }
        
        public GBrush()
        {
            Inherits = "CompositionBrush";
        }
    }

    public class GList : GClass
    {
        [XmlAttribute]
        public string ItemType { get; set; }
    }

    public class GProperty
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Type { get; set; }
        [XmlAttribute]
        public string DefaultValue { get; set; }
        [XmlAttribute]
        public bool Animated { get; set; }
        [XmlAttribute]
        public bool InternalSet { get; set; }
        [XmlAttribute]
        public bool Internal { get; set; }
    }

    public class GAnimationType
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Type { get; set; }
    }
}