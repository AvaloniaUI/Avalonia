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
    public class DataRepeaterRow : DataRepeaterDockPanel
    {
        internal static readonly DirectProperty<DataRepeaterRow, DataRepeaterHeaderDescriptors> HeaderDescriptorsProperty =
            AvaloniaProperty.RegisterDirect<DataRepeaterRow, DataRepeaterHeaderDescriptors>(
                nameof(HeaderDescriptors),
                o => o.HeaderDescriptors,
                (o, v) => o.HeaderDescriptors = v);

        private DataRepeaterHeaderDescriptors _headerDescriptors;

        internal DataRepeaterHeaderDescriptors HeaderDescriptors
        {
            get => _headerDescriptors;
            set
            {
                SetAndRaise(HeaderDescriptorsProperty, ref _headerDescriptors, value);
            }
        }

        public DataRepeaterRow()
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
            if (e.PropertyName == nameof(DataRepeaterHeaderDescriptor.HeaderWidth))
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
                var target = _curCells[index];

                if (target.Classes.Contains("LastColumn"))
                    continue;

                if (target._cellContent is DataRepeaterCellContent cell)
                {
                    if (cell.Width != desc.HeaderWidth)
                        cell.Width = desc.HeaderWidth;
                }
            }
        }

        CompositeDisposable _disposables;

        List<DataRepeaterCell> _curCells = new List<DataRepeaterCell>();

        private void DescriptorsChanged(DataRepeaterHeaderDescriptors obj)
        {
            if (obj == null) return;

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            Children.Clear();


            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                headerDesc.PropertyChanged += HeaderDescriptorsChanged;

                var cell = new DataRepeaterCell
                {
                    TargetProperty = headerDesc.PropertyName
                };

                cell.Classes.Add(headerDesc.PropertyName);

                if (i + 1 == obj.Count)
                {
                    cell.Classes.Add("LastColumn");
                }

                _curCells.Add(cell);
                Children.Add(cell);
            }
        }
    }
}
