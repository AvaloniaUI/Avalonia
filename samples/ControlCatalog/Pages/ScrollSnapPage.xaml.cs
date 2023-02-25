using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using MiniMvvm;

namespace ControlCatalog.Pages
{
    public class ScrollSnapPageViewModel : ViewModelBase
    {
        private SnapPointsType _snapPointsType;
        private SnapPointsAlignment _snapPointsAlignment;
        private bool _areSnapPointsRegular;

        public ScrollSnapPageViewModel()
        {

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
        }

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

    public class ScrollSnapPage : UserControl
    {
        public ScrollSnapPage()
        {
            this.InitializeComponent();

            DataContext = new ScrollSnapPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
