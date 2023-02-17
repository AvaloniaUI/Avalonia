using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates
{
    public class TreeDataTemplate : ITreeDataTemplate, ITypedDataTemplate
    {
        [DataType]
        public Type DataType { get; set; }

        [Content]
        [TemplateContent]
        public object Content { get; set; }

        [AssignBinding]
        public BindingBase ItemsSource { get; set; }

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

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "If ItemsSource is a CompiledBinding, then path members will be preserver")]
        public IBinding ItemsSelector(object item)
        {
            return ItemsSource;
        }

        public Control Build(object data)
        {
            var visualTreeForItem = TemplateContent.Load(Content)?.Control;
            if (visualTreeForItem != null)
            {
                visualTreeForItem.DataContext = data;
            }

            return visualTreeForItem;
        }
    }
}
