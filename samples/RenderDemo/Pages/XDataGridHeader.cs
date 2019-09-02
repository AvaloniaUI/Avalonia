 


using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace RenderDemo.Pages
{
    public class XDataGridHeader : Grid
    {
        internal static readonly DirectProperty<XDataGridHeader, XDataGridHeaderDescriptors> HeaderDescriptorsProperty =
            AvaloniaProperty.RegisterDirect<XDataGridHeader, XDataGridHeaderDescriptors>(
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

        public XDataGridHeader()
        {
            this.WhenAnyValue(x => x.HeaderDescriptors)
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

				var boundCellContent = new XDataGridHeaderCell();

				boundCellContent.DataContext = headerDesc;

                Grid.SetColumn(boundCellContent, headerDesc.ColumnDefinitionIndex);

                this.Children.Add(boundCellContent);
            }
        }
    }
}
