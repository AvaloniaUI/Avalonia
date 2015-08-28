namespace Perspex.Markup.Xaml.Templates
{
    using System;
    using Controls.Templates;
    using OmniXaml.Attributes;
    using Controls;

    [ContentProperty("Content")]
    public class XamlDataTemplate : IDataTemplate
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