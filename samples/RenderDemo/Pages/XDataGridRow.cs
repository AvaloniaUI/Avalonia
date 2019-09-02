using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace RenderDemo.Pages
{
    public class XDataGridRow : Grid
    {
        internal static readonly DirectProperty<XDataGridRow, XDataGridHeaderDescriptors> HeaderDescriptorsProperty =
            AvaloniaProperty.RegisterDirect<XDataGridRow, XDataGridHeaderDescriptors>(
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

        public XDataGridRow()
        {
            this.WhenAnyValue(x=>x.HeaderDescriptors)
                .DistinctUntilChanged()
                .Subscribe(XD);
        }

        private void XD(XDataGridHeaderDescriptors obj)
        {
            DescriptorsChanged(obj);
        }
 
        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            if (obj == null) return;

            this.ColumnDefinitions.Clear();
            this.Children.Clear();

            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var colDefHeaderCell = new ColumnDefinition(new GridLength(100)); //temporary

                this.ColumnDefinitions.Add(colDefHeaderCell);

				var boundCellContent = new XDataGridCell();
				var newBind = new Binding(headerDesc.PropertyName);
				boundCellContent.Bind(XDataGridCell.ContentProperty, newBind);

                Grid.SetColumn(boundCellContent, headerDesc.ColumnDefinitionIndex);

                this.Children.Add(boundCellContent);
            }
        }
    }
}
