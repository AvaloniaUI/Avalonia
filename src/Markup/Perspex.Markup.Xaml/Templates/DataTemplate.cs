// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using OmniXaml.Attributes;
using Perspex.Controls;
using Perspex.Controls.Templates;

namespace Perspex.Markup.Xaml.Templates
{
    public class DataTemplate : IDataTemplate
    {
        public Type DataType { get; set; }
        public TemplateContent Content { get; set; }

        public bool Match(object data)
        {
            if (DataType == null)
            {
                throw new InvalidOperationException("DataTemplate must have a DataType.");
            }

            return DataType.GetTypeInfo().IsAssignableFrom(data.GetType().GetTypeInfo());
        }

        public IControl Build(object data)
        {
            var visualTreeForItem = Content.Load();
            visualTreeForItem.DataContext = data;
            return visualTreeForItem;
        }
    }
}