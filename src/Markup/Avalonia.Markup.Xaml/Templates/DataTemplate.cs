using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates
{
    public class DataTemplate : IDataTemplate
    {
        public Type DataType { get; set; }

        //we need content to be object otherwise portable.xaml is crashing
        [Content]
        [TemplateContent]
        public object Content { get; set; }

        public bool SupportsRecycling { get; set; } = true;

        public bool Match(object data)
        {
            if (DataType == null)
            {
                return true;
            }
            else
            {
                return DataType.IsInstanceOfType(data);
            }
        }

        public IControl Build(object data) => TemplateContent.Load(Content).Control;
    }
}
