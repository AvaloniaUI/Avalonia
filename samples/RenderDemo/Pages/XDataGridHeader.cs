


using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
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

        static GridLength StarGridLength = GridLength.Parse("100*");

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

        CompositeDisposable _disposables;

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

                var colDefHeaderCol = new ColumnDefinition(StarGridLength);
                this.ColumnDefinitions.Add(colDefHeaderCol);

                colDefHeaderCol.WhenAnyValue(x => x.Width)
                               .Throttle(TimeSpan.FromSeconds(1/24))
                               .DistinctUntilChanged()
                               .ObserveOn(RxApp.MainThreadScheduler)
                               .Do(x => headerDesc.HeaderWidth = x)
                               .Subscribe()
                               .DisposeWith(_disposables);

                var colDefHeaderResizer = new ColumnDefinition(GridLength.Parse("5"));
                this.ColumnDefinitions.Add(colDefHeaderResizer);

                var boundCellContent = new XDataGridHeaderCell();

                boundCellContent.DataContext = headerDesc;

                var resizer = new GridSplitter();

                Grid.SetColumn(boundCellContent, actualColIndex);
                Grid.SetColumn(resizer, actualColIndex + 1);

                this.Children.Add(boundCellContent);
                if (i + 1 != obj.Count)
                    this.Children.Add(resizer);

                actualColIndex += 2;
            }

            var colDefExtra = new ColumnDefinition(StarGridLength);
            this.ColumnDefinitions.Add(colDefExtra);
        }
    }
}
