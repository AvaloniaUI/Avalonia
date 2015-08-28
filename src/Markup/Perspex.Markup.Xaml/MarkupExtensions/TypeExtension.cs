// -----------------------------------------------------------------------
// <copyright file="TypeExtension.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.MarkupExtensions
{
    using System;
    using Glass;
    using OmniXaml;
    using OmniXaml.Attributes;
    using OmniXaml.Typing;

    [ContentProperty("TargetType")]
    public class TypeExtension : MarkupExtension
    {
        public Type Type { get; set; }

        public TypeExtension()
        {
        }

        public TypeExtension(Type type)
        {
            this.Type = type;
        }

        public string TypeName { get; set; }

        private Type ResolveFromString(string typeLocator, IXamlTypeRepository typeRepository)
        {
            Guard.ThrowIfNull(typeLocator, nameof(typeLocator));

            var prefixAndType = typeLocator.Dicotomize(':');

            var xamlType = typeRepository.GetByPrefix(prefixAndType.Item1, prefixAndType.Item2);
            return xamlType.UnderlyingType;
        }

        public override object ProvideValue(MarkupExtensionContext markupExtensionContext)
        {
            if (this.Type != null)
            {
                return this.Type;
            }

            return this.ResolveFromString(this.TypeName, markupExtensionContext.TypeRepository);
        }
    }
}