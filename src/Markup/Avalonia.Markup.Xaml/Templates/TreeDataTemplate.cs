using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Markup.Xaml.Templates
{
    public class TreeDataTemplate : ITreeDataTemplate, ITypedDataTemplate
    {
        [DataType]
        public Type? DataType { get; set; }

        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        [AssignBinding]
        public BindingBase? ItemsSource { get; set; }

        public bool Match(object? data)
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

        public IDisposable BindChildren(AvaloniaObject target, AvaloniaProperty targetProperty, object item)
        {
            return ItemsSource is not null ?
                target.Bind(targetProperty, ItemsSource) :
                Disposable.Empty;
        }

        public Control? Build(object? data)
        {
            var visualTreeForItem = TemplateContent.Load(Content)?.Result;
            if (visualTreeForItem != null)
            {
                visualTreeForItem.DataContext = data;
            }

            return visualTreeForItem;
        }
    }
}
