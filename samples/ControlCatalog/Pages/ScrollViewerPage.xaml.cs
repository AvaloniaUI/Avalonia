using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using MiniMvvm;

namespace ControlCatalog.Pages
{
    public class ScrollViewerPageViewModel : ViewModelBase
    {
        private bool _allowAutoHide;
        private ScrollBarVisibility _horizontalScrollVisibility;
        private ScrollBarVisibility _verticalScrollVisibility;
        private SnapPointsType _verticalSnapPointsType;
        private SnapPointsAlignment _verticalSnapPointsAlignment;
        private bool _areSnapPointsRegular;

        public ScrollViewerPageViewModel()
        {
            AvailableVisibility = new List<ScrollBarVisibility>
            {
                ScrollBarVisibility.Auto,
                ScrollBarVisibility.Visible,
                ScrollBarVisibility.Hidden,
                ScrollBarVisibility.Disabled,
            };

            AvailableSnapPointsType = new List<SnapPointsType>()
            { 
                SnapPointsType.None,
                SnapPointsType.Mandatory,
                SnapPointsType.MandatorySingle
            };

            AvailableSnapPointsAlignment = new List<SnapPointsAlignment>()
            {
                SnapPointsAlignment.Near,
                SnapPointsAlignment.Center,
                SnapPointsAlignment.Far,
            };

            HorizontalScrollVisibility = ScrollBarVisibility.Auto;
            VerticalScrollVisibility = ScrollBarVisibility.Auto;
            AllowAutoHide = true;
        }

        public bool AllowAutoHide
        {
            get => _allowAutoHide;
            set => this.RaiseAndSetIfChanged(ref _allowAutoHide, value);
        }

        public bool AreSnapPointsRegular
        {
            get => _areSnapPointsRegular;
            set => this.RaiseAndSetIfChanged(ref _areSnapPointsRegular, value);
        }

        public ScrollBarVisibility HorizontalScrollVisibility
        {
            get => _horizontalScrollVisibility;
            set => this.RaiseAndSetIfChanged(ref _horizontalScrollVisibility, value);
        }

        public ScrollBarVisibility VerticalScrollVisibility
        {
            get => _verticalScrollVisibility;
            set => this.RaiseAndSetIfChanged(ref _verticalScrollVisibility, value);
        }

        public SnapPointsType VerticalSnapPointsType
        {
            get => _verticalSnapPointsType;
            set => this.RaiseAndSetIfChanged(ref _verticalSnapPointsType, value);
        }

        public SnapPointsAlignment VerticalSnapPointsAlignment
        {
            get => _verticalSnapPointsAlignment;
            set => this.RaiseAndSetIfChanged(ref _verticalSnapPointsAlignment, value);
        }

        public List<ScrollBarVisibility> AvailableVisibility { get; }
        public List<SnapPointsType> AvailableSnapPointsType { get; }
        public List<SnapPointsAlignment> AvailableSnapPointsAlignment { get; }
    }

    public class ScrollViewerPage : UserControl
    {
        public ScrollViewerPage()
        {
            InitializeComponent();

            DataContext = new ScrollViewerPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
