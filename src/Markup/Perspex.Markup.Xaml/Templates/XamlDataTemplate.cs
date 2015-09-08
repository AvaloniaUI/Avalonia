// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml.Attributes;
using Perspex.Controls;
using Perspex.Controls.Templates;

namespace Perspex.Markup.Xaml.Templates
{
    [ContentProperty("Content")]
    public class XamlDataTemplate : IDataTemplate
    {
        private bool MyMatch(object data)
        {
            if (this.DataType == null)
            {
                throw new InvalidOperationException("XAML DataTemplates must have a DataType");
            }

            return this.DataType == data.GetType();
        }

        private Control CreateVisualTreeForItem(object data)
        {
            var visualTreeForItem = this.Content.Load();
            visualTreeForItem.DataContext = data;
            return visualTreeForItem;
        }

        public Type DataType { get; set; }

        public TemplateContent Content { get; set; }

        public IControl Build(object param)
        {
            return this.CreateVisualTreeForItem(param);
        }

        public bool Match(object data)
        {
            return this.MyMatch(data);
        }
    }
}