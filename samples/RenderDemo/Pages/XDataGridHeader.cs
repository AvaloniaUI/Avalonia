


using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RenderDemo.Pages
{
    public class XDataGridHeader : XDataGridDockPanel
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

        CompositeDisposable _disposables;

        public XDataGridHeader()
        {
            this.WhenAnyValue(x => x.HeaderDescriptors)
                .DistinctUntilChanged()
                .Subscribe(DescriptorsChanged);
        }

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            if (obj == null) return;

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            Children.Clear();

            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var boundCellContent = new XDataGridHeaderCell
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
