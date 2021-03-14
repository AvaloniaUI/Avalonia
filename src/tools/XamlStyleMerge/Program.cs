using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XamlStyleMerge
{
    class Program
    {
        private static readonly XNamespace s_ns = "https://github.com/avaloniaui";
        private static readonly XNamespace s_xns = "http://schemas.microsoft.com/winfx/2006/xaml";
        private static readonly List<XAttribute> s_namespaces = new List<XAttribute>();
        private static readonly OrderedDictionary s_resources = new OrderedDictionary();
        private static readonly List<XElement> s_styles = new List<XElement>();
        private static string s_basePath = null!;

        static void Main(string[] args)
        {
            if (args.Length == 0)
                Console.WriteLine("Usage: XamlStyleMerge <input> [output]");

            if (!UriParser.IsKnownScheme("avares"))
                UriParser.Register(new GenericUriParser(
                    GenericUriParserOptions.GenericAuthority |
                    GenericUriParserOptions.NoUserInfo |
                    GenericUriParserOptions.NoPort |
                    GenericUriParserOptions.NoQuery |
                    GenericUriParserOptions.NoFragment), "avares", -1);

            s_basePath = Path.GetDirectoryName(args[0])!;

            var f = File.ReadAllText(args[0]);
            var d = XDocument.Parse(f);

            if (d.Root.Name != s_ns + "Styles")
            {
                throw new InvalidOperationException("File is not an Avalonia Styles document.");
            }

            ProcessStyles(d.Root);

            foreach (var ns in s_namespaces)
            {
                if (d.Root.Attribute(ns.Name) is null)
                    d.Root.Add(ns);
            }

            d.Root.RemoveNodes();
            d.Root.Add(new XElement(s_ns + "Styles.Resources", s_resources.Values));
            d.Root.Add(s_styles);

            var outFile = args.Length > 1 ? args[1] : "out.xaml";
            d.Save(outFile);
        }

        private static void Process(string fileName)
        {
            var f = File.ReadAllText(fileName);
            var d = XDocument.Parse(f);

            if (d.Root.Name == s_ns + "Style")
                ProcessStyle(d.Root);
            else
                ProcessStyles(d.Root);
        }

        private static void ProcessStyle(XElement element)
        {
            ProcessAttributes(element);
            ProcessResources(element);
            RemoveDesignTimeElements(element);

            var selector = element.Attribute("Selector");

            if (selector is object)
            {

                s_styles.Add(element);
            }
        }

        private static void ProcessStyles(XElement element)
        {
            ProcessAttributes(element);
            ProcessResources(element);
            RemoveDesignTimeElements(element);

            foreach (var child in element.Elements(s_ns + "StyleInclude"))
            {
                var source = new Uri(child.Attribute("Source").Value);
                var fileName = Path.Combine(s_basePath, source.LocalPath.TrimStart('/'));
                Process(fileName);
            }

            foreach (var child in element.Elements(s_ns + "Style"))
            {
                ProcessStyle(child);
            }
        }

        private static void ProcessAttributes(XElement element)
        {
            foreach (var attr in element.Attributes().ToList())
            {
                if (attr.IsNamespaceDeclaration)
                {
                    s_namespaces.Add(attr);
                    attr.Remove();
                }
            }
        }

        private static void ProcessResources(XElement element)
        {
            var resources = element.Element(s_ns + "Style.Resources") ??
                element.Element(s_ns + "Styles.Resources");

            if (resources is object)
            {
                foreach (var r in resources.Elements())
                {
                    var key = r.Attribute(s_xns + "Key").Value;
                    s_resources[key] = r;
                }

                resources.ReplaceWith(null);
            }
        }

        private static void RemoveDesignTimeElements(XElement element)
        {
            foreach (var e in element.Elements(s_ns + "Design.PreviewWith"))
                e.Remove();
        }
    }
}
