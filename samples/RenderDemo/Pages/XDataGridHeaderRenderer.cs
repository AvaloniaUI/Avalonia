using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using System.Linq;

namespace RenderDemo.Pages
{
    public class XDataGridHeaderRenderer : Grid
    {
        internal static readonly DirectProperty<XDataGridHeaderRenderer, XDataGridHeaderDescriptors> HeaderDescriptorsProperty =
            AvaloniaProperty.RegisterDirect<XDataGridHeaderRenderer, XDataGridHeaderDescriptors>(
                nameof(HeaderDescriptors),
                o => o.HeaderDescriptors,
                (o, v) => o.HeaderDescriptors = v);

        private XDataGridHeaderDescriptors _headerDescriptors;

        internal XDataGridHeaderDescriptors HeaderDescriptors
        {
            get => _headerDescriptors;
            set
            {
                SetAndRaise(HeaderDescriptorsProperty, ref _headerDescriptors, value);

            }
        }

        public XDataGridHeaderRenderer()
        {
            HeaderDescriptorsProperty.Changed.AddClassHandler<XDataGridHeaderRenderer>(HeaderDescriptorsChanged);
        }

        private void HeaderDescriptorsChanged(AvaloniaObject arg1, AvaloniaPropertyChangedEventArgs arg2)
        {
            var descriptor = arg2.NewValue as XDataGridHeaderDescriptors;
            if (descriptor is null) return;

            DescriptorsChanged(descriptor);
        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            this.Children.Clear();

            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var colDefHeaderCell = new ColumnDefinition(new GridLength(100)); //temporary

                this.ColumnDefinitions.Add(colDefHeaderCell);

                var rowValue = headerDesc.PropertyName;

                var boundCellContent = new XDataGridCell();

                boundCellContent.Content = new TextBlock() { Text = rowValue };

                Grid.SetColumn(boundCellContent, headerDesc.ColumnDefinitionIndex);

                this.Children.Add(boundCellContent);
            }
        }

    }
}
