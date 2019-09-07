using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RenderDemo.Pages
{
    public class XDataGridRow : XDataGridDockPanel
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

            this.WhenAnyValue(x => x.DataContext)
                .Subscribe(x =>
                {
                    RefreshRowWidths();
                });
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            RefreshRowWidths();

            base.ArrangeCore(finalRect);
        }

        private void HeaderDescriptorsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XDataGridHeaderDescriptor.HeaderWidth))
            {
                RefreshRowWidths();
            }
        }

        private void RefreshRowWidths()
        {
            if (HeaderDescriptors is null || _curCells is null) return;

            foreach (var desc in HeaderDescriptors)
            {
                var index = HeaderDescriptors.IndexOf(desc);

                if (_curCells[index]._contentControl is ContentControl cell)
                {
                    if (cell.Width != desc.HeaderWidth)
                        cell.Width = desc.HeaderWidth;
                }
            }
        }

        CompositeDisposable _disposables;

        List<XDataGridCell> _curCells = new List<XDataGridCell>();

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            if (obj == null) return;

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            this.Children.Clear();


            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                headerDesc.PropertyChanged += HeaderDescriptorsChanged;

                var cell = new XDataGridCell();

                var newBind = new Binding(headerDesc.PropertyName, BindingMode.TwoWay);

                cell.Bind(XDataGridCell.ContentProperty, newBind);

                if (i + 1 == obj.Count)
                {
                    cell.Classes.Add("LastColumn");
                }

                _curCells.Add(cell);
                this.Children.Add(cell);
            }

        }
    }
}
