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
        private bool _enableInertia;
        private ScrollBarVisibility _horizontalScrollVisibility;
        private ScrollBarVisibility _verticalScrollVisibility;
        private SnapPointsType _snapPointsType;
        private SnapPointsAlignment _snapPointsAlignment;
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
            EnableInertia = true;
        }

        public bool AllowAutoHide
        {
            get => _allowAutoHide;
            set => this.RaiseAndSetIfChanged(ref _allowAutoHide, value);
        }

        public bool EnableInertia
        {
            get => _enableInertia;
            set => this.RaiseAndSetIfChanged(ref _enableInertia, value);
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

        public List<ScrollBarVisibility> AvailableVisibility { get; }

        public bool AreSnapPointsRegular
        {
            get => _areSnapPointsRegular;
            set => this.RaiseAndSetIfChanged(ref _areSnapPointsRegular, value);
        }

        public SnapPointsType SnapPointsType
        {
            get => _snapPointsType;
            set => this.RaiseAndSetIfChanged(ref _snapPointsType, value);
        }

        public SnapPointsAlignment SnapPointsAlignment
        {
            get => _snapPointsAlignment;
            set => this.RaiseAndSetIfChanged(ref _snapPointsAlignment, value);
        }
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
