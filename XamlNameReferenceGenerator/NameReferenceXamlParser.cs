using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.CodeAnalysis;

namespace XamlNameReferenceGenerator
{
    internal class NameReferenceXamlParser
    {
        public List<(string TypeName, string Name)> GetNamedControls(AdditionalText additionalText)
        {
            var xaml = additionalText.GetText()!.ToString();
            var document = new XmlDocument();
            document.LoadXml(xaml);

            var names = new List<(string TypeName, string Name)>();
            IterateThroughAllNodes(document, node =>
            {
                var type = node.Name;
                if (type.Contains(":"))
                    type = type.Split(':')[1];

                var name = node.Attributes?["x:Name"]?.Value ??
                           node.Attributes?["Name"]?.Value;
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add((type, name));
            });
            return names;
        }

        private static void IterateThroughAllNodes(XmlDocument doc, Action<XmlNode> elementVisitor)
        {
            foreach (XmlNode node in doc.ChildNodes)
            {
                IterateNode(node, elementVisitor);
            }
        }

        private static void IterateNode(XmlNode node, Action<XmlNode> elementVisitor)
        {
            elementVisitor(node);
            foreach (XmlNode childNode in node.ChildNodes)
            {
                IterateNode(childNode, elementVisitor);
            }
        }
    }
}