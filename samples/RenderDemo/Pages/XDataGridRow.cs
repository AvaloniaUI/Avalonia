using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
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

        CompositeDisposable _disposables;

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {

        }

        static GridLength StarGridLength = GridLength.Parse("100*");

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            if (obj == null) return;

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            
            this.ColumnDefinitions.Clear();
            this.Children.Clear();
            var actualColIndex = 0;

            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var colDefCellCol = new ColumnDefinition(headerDesc.HeaderWidth); //temporary
                var colDefHeaderResizer = new ColumnDefinition(GridLength.Parse("5"));

                headerDesc.WhenAnyValue(x => x.HeaderWidth)
                          .DistinctUntilChanged()
                          .Do(x =>
                          {
                              colDefCellCol.Width = x;
                          })
                          .Subscribe()
                          .DisposeWith(_disposables);

                this.ColumnDefinitions.Add(colDefCellCol);
                this.ColumnDefinitions.Add(colDefHeaderResizer);

                var boundCellContent = new XDataGridCell();

                var newBind = new Binding(headerDesc.PropertyName);
                boundCellContent.Bind(XDataGridCell.ContentProperty, newBind);

                Grid.SetColumn(boundCellContent, actualColIndex);

                this.Children.Add(boundCellContent);

                actualColIndex += 2;
            }

            var colDefExtra = new ColumnDefinition(StarGridLength);
            this.ColumnDefinitions.Add(colDefExtra);
        }
    }
}
