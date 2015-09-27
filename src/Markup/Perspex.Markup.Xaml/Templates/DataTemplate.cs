// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml.Attributes;
using Perspex.Controls;
using Perspex.Controls.Templates;

namespace Perspex.Markup.Xaml.Templates
{
    [ContentProperty("Content")]
    public class DataTemplate : IDataTemplate
    {
        private bool MyMatch(object data)
        {
            if (DataType == null)
            {
                throw new InvalidOperationException("XAML DataTemplates must have a DataType");
            }

            return DataType == data.GetType();
        }

        private Control CreateVisualTreeForItem(object data)
        {
            var visualTreeForItem = Content.Load();
            visualTreeForItem.DataContext = data;
            return visualTreeForItem;
        }

        public Type DataType { get; set; }

        public TemplateContent Content { get; set; }

        public IControl Build(object param)
        {
            return CreateVisualTreeForItem(param);
        }

        public bool Match(object data)
        {
            return MyMatch(data);
        }
    }
}