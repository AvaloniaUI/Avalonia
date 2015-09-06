using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace Perspex.Markup.Xaml.Schema.Generator
{
    public class Program
    {
        private const string Xs = "http://www.w3.org/2001/XMLSchema";

        const string Namespace = "https://github.com/grokys/Perspex";
        private static readonly XmlQualifiedName StringType = new XmlQualifiedName("string", Xs);
        private const string TypeSuffix = "PerspexType";
        static void Main(string[] args)
        {
            var outputFile = args.Length > 0 ? args[0] : @"..\..\perspex.xsd";

            var schema = new XmlSchema()
            {
                TargetNamespace = Namespace, ElementFormDefault = XmlSchemaForm.Qualified
            };

            var globalChoice = new XmlSchemaChoice();
            schema.Items.Add(new XmlSchemaGroup() {Name = "all", Particle = globalChoice});
            var declared = new HashSet<string>();
            foreach (var dll in
                Directory.GetFiles(Path.Combine(typeof (Program).Assembly.GetModules()[0].FullyQualifiedName, ".."),
                    "*.dll"))
            {
                foreach (var t in Assembly.LoadFrom(dll).GetTypes())
                {
                    if (typeof (PerspexObject).IsAssignableFrom(t))
                    {
                        if (!declared.Add(t.Name))
                            continue;
                        var ctype = new XmlSchemaComplexType {Name = t.Name + TypeSuffix, IsMixed = true};
                        schema.Items.Add(ctype);
                        var element = new XmlSchemaElement()
                        {
                            SchemaTypeName = new XmlQualifiedName(t.Name + TypeSuffix, Namespace),
                            Name = t.Name
                        };
                        schema.Items.Add(element);
                        globalChoice.Items.Add(new XmlSchemaElement()
                        {
                            RefName = new XmlQualifiedName(element.Name, Namespace)
                        });

                        var sequence = new XmlSchemaChoice() {MaxOccursString = "unbounded"};
                        ctype.Particle = sequence;
                        var hs = new HashSet<string>();
                        foreach (var prop in t.GetProperties())
                        {
                            if (!hs.Add(prop.Name))
                                continue;
                            var propType = prop.PropertyType;
                            var name = prop.Name;

                            ctype.Attributes.Add(new XmlSchemaAttribute()
                            {
                                Name = prop.Name,
                                Use = XmlSchemaUse.Optional,
                                SchemaTypeName = ResolveTypeName(propType, true)
                            });
                            var subElement = new XmlSchemaElement()
                            {
                                // ReSharper disable once PossibleNullReferenceException
                                Name = prop.DeclaringType.Name + "." + prop.Name,
                                SchemaTypeName = ResolveTypeName(propType, false),
                                MaxOccurs = 1,
                                MinOccurs = 0
                            };
                            sequence.Items.Add(subElement);
                        }
                        sequence.Items.Add(new XmlSchemaGroupRef() {RefName = new XmlQualifiedName("all", Namespace)});
                    }
                    if (t.IsEnum)
                    {
                        if (!declared.Add(t.Name))
                            continue;

                        const string EnumTypeSuffix = "Enumeration";

                        var restriction = new XmlSchemaSimpleTypeRestriction() {BaseTypeName = StringType};
                        foreach (var name in Enum.GetNames(t))
                            restriction.Facets.Add(new XmlSchemaEnumerationFacet()
                            {
                                Value = name
                            });
                        schema.Items.Add(new XmlSchemaSimpleType
                        {
                            Content = restriction,
                            Name = t.Name + TypeSuffix + EnumTypeSuffix
                        });

                        schema.Items.Add(new XmlSchemaSimpleType()
                        {
                            Name = t.Name + TypeSuffix,
                            
                            Content = new XmlSchemaSimpleTypeUnion()
                            {
                                MemberTypes =new[]
                                {
                                    new XmlQualifiedName(t.Name + TypeSuffix + EnumTypeSuffix, Namespace),
                                    new XmlQualifiedName("bindingPattern", Namespace), 
                                }
                            }
                        });

                    }
                }
            }

            schema.Items.Add(new XmlSchemaSimpleType()
            {
                Name = "bindingPattern",
                Content = new XmlSchemaSimpleTypeRestriction()
                {
                    BaseTypeName = StringType,
                    Facets =
                    {
                        new XmlSchemaPatternFacet()
                        {
                            Value = "^{.*}$"
                        }
                    }
                }
            });

            
            var schemaSet = new XmlSchemaSet();
            schemaSet.ValidationEventHandler += ValidationCallback;
            schemaSet.Add(schema);
            schemaSet.Compile();

            foreach (XmlSchema s in schemaSet.Schemas())
                schema = s;

            using (var fs = new StreamWriter(outputFile))
                schema.Write(fs);
        }

        private static XmlQualifiedName ResolveTypeName(Type type, bool attribute)
        {
            if ((!attribute && typeof (PerspexObject).IsAssignableFrom(type)) || (attribute && type.IsEnum))
                return new XmlQualifiedName(type.Name + TypeSuffix, Namespace);

            return StringType;
        }


        private static void ValidationCallback(object sender, ValidationEventArgs e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(1);
        }
    }
}
