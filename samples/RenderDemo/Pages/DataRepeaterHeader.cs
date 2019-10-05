


using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RenderDemo.Pages
{
    public class DataRepeaterHeader : DataRepeaterDockPanel
    {
        internal static readonly DirectProperty<DataRepeaterHeader, DataRepeaterHeaderDescriptors> HeaderDescriptorsProperty =
            AvaloniaProperty.RegisterDirect<DataRepeaterHeader, DataRepeaterHeaderDescriptors>(
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

        CompositeDisposable _disposables;

        public DataRepeaterHeader()
        {
            this.WhenAnyValue(x => x.HeaderDescriptors)
                .DistinctUntilChanged()
                .Subscribe(DescriptorsChanged);
        }

        private void DescriptorsChanged(DataRepeaterHeaderDescriptors obj)
        {
            if (obj == null) return;

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            Children.Clear();

            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var boundCellContent = new DataRepeaterHeaderCell
                {
                    Content = headerDesc
                };

                if (i + 1 == obj.Count)
                {
                    boundCellContent.Classes.Add("LastColumn");
                }

                Children.Add(boundCellContent);
            }
        }
    }
}
