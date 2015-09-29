// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using OmniXaml.Attributes;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Markup.Xaml.DataBinding;

namespace Perspex.Markup.Xaml.Templates
{
    [ContentProperty("Content")]
    public class TreeDataTemplate : ITreeDataTemplate
    {
        public Type DataType { get; set; }
        public TemplateContent Content { get; set; }
        public XamlBinding ItemsSource { get; set; }

        public bool Match(object data)
        {
            if (DataType == null)
            {
                throw new InvalidOperationException("DataTemplate must have a DataType.");
            }

            return DataType == data.GetType();
        }

        public IEnumerable ItemsSelector(object item)
        {
            if (ItemsSource != null)
            {
                // TODO: Get value of ItemsSource here.
            }

            return null;
        }

        public bool IsExpanded(object item)
        {
            return true;
        }

        public IControl Build(object data)
        {
            var visualTreeForItem = Content.Load();
            visualTreeForItem.DataContext = data;
            return visualTreeForItem;
        }
    }
}