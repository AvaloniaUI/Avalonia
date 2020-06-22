using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Parsers;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates
{
    public class TreeDataTemplate : ITreeDataTemplate
    {
        public Type DataType { get; set; }

        [Content]
        [TemplateContent]
        public object Content { get; set; }

        [AssignBinding]
        public Binding ItemsSource { get; set; }

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

        public InstancedBinding ItemsSelector(object item)
        {
            if (ItemsSource != null)
            {
                var obs = ExpressionObserverBuilder.Build(item, ItemsSource.Path);
                return InstancedBinding.OneWay(obs, BindingPriority.Style);
            }

            return null;
        }

        public IControl Build(object data)
        {
            var visualTreeForItem = TemplateContent.Load(Content).Control;
            visualTreeForItem.DataContext = data;
            return visualTreeForItem;
        }
    }
}
