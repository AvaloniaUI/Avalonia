using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RenderDemo.Pages
{
    public class XDataGridRow : StackPanel
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
                .Subscribe(DescriptorsChanged);
        }
 
        CompositeDisposable _disposables;

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {

        }

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            if (obj == null) return;

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            
            // this.ColumnDefinitions.Clear();
            this.Children.Clear();
            // var actualColIndex = 0;

            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var boundCellContent = new XDataGridCell();

                headerDesc.WhenAnyValue(x => x.HeaderWidth)
                          .DistinctUntilChanged()
                          .Do(x =>
                          {
                              boundCellContent.CellContentWidth = x;
                          })
                          .Subscribe()
                          .DisposeWith(_disposables);
 

                var newBind = new Binding(headerDesc.PropertyName);
                boundCellContent.Bind(XDataGridCell.ContentProperty, newBind);

                this.Children.Add(boundCellContent);
            }
        }
    }
}
