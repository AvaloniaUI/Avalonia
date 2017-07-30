// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class TypeExtension : Portable.Xaml.Markup.TypeExtension
    {
        public TypeExtension()
        {
        }

        public TypeExtension(string typeName) : base(typeName)
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return base.ProvideValue(serviceProvider);
        }
    }
}