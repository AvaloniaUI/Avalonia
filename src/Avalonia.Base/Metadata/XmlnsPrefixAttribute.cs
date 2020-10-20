using System;

namespace Avalonia.Metadata
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsPrefixAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xmlNamespace">XML namespce</param>
        /// <param name="prefix">recommended prefix</param>
        public XmlnsPrefixAttribute(string xmlNamespace, string prefix)
        {
            XmlNamespace = xmlNamespace ?? throw new ArgumentNullException(nameof(xmlNamespace));

            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }

        /// <summary>
        /// XML Namespace
        /// </summary>
        public string XmlNamespace { get; }

        /// <summary>
        /// New Xml Namespace
        /// </summary>
        public string Prefix { get; }
    }
}
