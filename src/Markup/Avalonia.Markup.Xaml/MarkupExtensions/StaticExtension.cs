// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
#if !OMNIXAML

    //TODO: check do we need something more than std Portable.xaml static??
    public class StaticExtension : Portable.Xaml.Markup.StaticExtension
    {
        public StaticExtension()
        {
        }

        public StaticExtension(string member)
            : base(member)
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var tdc = (ITypeDescriptorContext)serviceProvider;
            var ns = tdc.GetService<IXamlNamespaceResolver>();

            if (!ns.GetNamespacePrefixes().Any())
            {
               //TODO: issue of portable.xaml ??? investigate
                var cvp = tdc.GetService<IRootObjectProvider>();
                var pvt = tdc.GetService<IProvideValueTarget>(); 
                //ok we have a problem namespaces shouldn't be empty
                var nslist = ns.GetNamespacePrefixes() as IList<NamespaceDeclaration>;
                nslist.Add(new NamespaceDeclaration("https://github.com/avaloniaui", ""));
                //this is just a hack
            }
            return base.ProvideValue(serviceProvider);
        }
    }

#else

    using System.Linq;
    using System.Reflection;
    using OmniXaml;
    using Glass.Core;

    public class StaticExtension : MarkupExtension
    {
        public StaticExtension()
        {
        }

        public StaticExtension(string identifier)
        {
            Identifier = identifier;
        }

        public string Identifier { get; set; }

        public override object ProvideValue(MarkupExtensionContext markupExtensionContext)
        {
            var typeRepository = markupExtensionContext.ValueContext.TypeRepository;
            var typeAndMember = GetTypeAndMember(Identifier);
            var prefixAndType = GetPrefixAndType(typeAndMember.Item1);
            var xamlType = typeRepository.GetByPrefix(prefixAndType.Item1, prefixAndType.Item2);
            return GetValue(xamlType.UnderlyingType, typeAndMember.Item2);
        }

        private static Tuple<string, string> GetTypeAndMember(string s)
        {
            var parts = s.Split('.');

            if (parts.Length != 2)
            {
                throw new ArgumentException("Static member must be in the form Type.Member.");
            }

            return Tuple.Create(parts[0], parts[1]);
        }

        private static Tuple<string, string> GetPrefixAndType(string s)
        {
            if (s.Contains(":"))
            {
                return s.Dicotomize(':');
            }
            else
            {
                return new Tuple<string, string>(string.Empty, s);
            }
        }

        private object GetValue(Type type, string name)
        {
            var t = type;

            while (t != null)
            {
                var result = t.GetTypeInfo().DeclaredMembers.FirstOrDefault(x => x.Name == name);

                if (result is PropertyInfo)
                {
                    var property = ((PropertyInfo)result);

                    if (property.GetMethod.IsStatic)
                    {
                        return ((PropertyInfo)result).GetValue(null);
                    }
                }
                else if (result is FieldInfo)
                {
                    return ((FieldInfo)result).GetValue(null);
                }

                t = t.GetTypeInfo().BaseType;
            }

            throw new ArgumentException($"Static member '{type}.{name}' not found.");
        }
    }
#endif
}