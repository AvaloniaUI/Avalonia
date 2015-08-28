// -----------------------------------------------------------------------
// <copyright file="XamlDataTemplate.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.Templates
{
    using System;
    using Controls.Templates;
    using OmniXaml.Attributes;
    using Perspex.Controls;

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