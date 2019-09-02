using Avalonia;
using Avalonia.Controls;
using System;

namespace RenderDemo.Pages
{
    public class XDataGridRowRenderer : Grid
    {
        internal static readonly DirectProperty<XDataGridRowRenderer, XDataGridHeaderDescriptors> HeaderDescriptorsProperty =
            AvaloniaProperty.RegisterDirect<XDataGridRowRenderer, XDataGridHeaderDescriptors>(
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

        public XDataGridRowRenderer()
        {
            HeaderDescriptorsProperty.Changed.AddClassHandler<XDataGridHeaderRenderer>(HeaderDescriptorsChanged);

        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            var descriptor = HeaderDescriptors;

            if (descriptor is null) return;

            DescriptorsChanged(descriptor);

            base.OnDataContextChanged(e);
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
            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var colDefHeaderCell = new ColumnDefinition(new GridLength(100)); //temporary

                this.ColumnDefinitions.Add(colDefHeaderCell);

                var rowValue = GetPropValue(this.DataContext, headerDesc.PropertyName);

                var boundCellContent = new XDataGridCell();

                boundCellContent.Content = rowValue;

                Grid.SetColumn(boundCellContent, headerDesc.ColumnDefinitionIndex);

                this.Children.Add(boundCellContent);
            }
        }

        private void VisualDetached(object sender, VisualTreeAttachmentEventArgs e)
        {

        }

        private void VisualAttached(object sender, VisualTreeAttachmentEventArgs e)
        {

        }
    }
}
