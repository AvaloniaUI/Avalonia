// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using OmniXaml.Attributes;
using OmniXaml.Typing;
using Glass.Core;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    [ContentProperty("TargetType")]
    public class TypeExtension : MarkupExtension
    {
        public Type Type { get; set; }

        public TypeExtension()
        {
        }

        public TypeExtension(Type type)
        {
            Type = type;
        }

        public string TypeName { get; set; }

        private Type ResolveFromString(string type, ITypeRepository typeRepository)
        {
            Guard.ThrowIfNull(type, nameof(type));

            var split = type.Split(':');
            var prefix = split.Length == 1 ? split[0] : null;
            var typeName = split.Length == 1 ? split[1] : split[0];
            var xamlType = typeRepository.GetByPrefix(prefix, typeName);
            return xamlType.UnderlyingType;
        }

        public override object ProvideValue(MarkupExtensionContext markupExtensionContext)
        {
            if (Type != null)
            {
                return Type;
            }

            return ResolveFromString(TypeName, markupExtensionContext.ValueContext.TypeRepository);
        }
    }
}