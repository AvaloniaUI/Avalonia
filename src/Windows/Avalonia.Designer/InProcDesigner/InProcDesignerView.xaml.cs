using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Avalonia.Designer.AppHost;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Avalonia.Designer.InProcDesigner
{
    /// <summary>
    /// Interaction logic for InProcDesignerView.xaml
    /// </summary>
    public partial class InProcDesignerView : UserControl
    {
        private readonly HostedAppModel _appModel;
        private readonly WindowHost _host;

        public InProcDesignerView(HostedAppModel appModel)
        {
            _appModel = appModel;
            InitializeComponent();
            DataContext = _appModel;
            _appModel.PropertyChanged += ModelPropertyChanged;
            WindowHostControl.Child = _host = new WindowHost(true);
            HandleVisibility();
            HandleWindow();

        }

        private void ModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HostedAppModel.Error) || e.PropertyName == nameof(HostedAppModel.ErrorDetails))
                HandleVisibility();
            if (e.PropertyName == nameof(HostedAppModel.NativeWindowHandle))
                HandleWindow();
        }

        private void HandleWindow()
        {
            _host.SetWindow(_appModel.NativeWindowHandle);
        }

        private void HandleVisibility()
        {
            ErrorPanel.Visibility = string.IsNullOrEmpty(_appModel.Error)
                       ? Visibility.Collapsed
                       : Visibility.Visible;
            DetailsBlock.Visibility = string.IsNullOrWhiteSpace(_appModel.ErrorDetails)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }



        public InProcDesignerView()
        {
            
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var wnd = new Window()
            {
                Content = new TextBox()
                {
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Text = _appModel.ErrorDetails
                }
            };
            wnd.ShowDialog();
        }

        private void ColorPicker_OnClick(object sender, MouseButtonEventArgs e)
        {
            var dlg = new ColorDialog() {Color = ColorTranslator.FromHtml(Settings.Background)};
            if (dlg.ShowDialog(_host) == DialogResult.OK)
            {
                var color = ColorTranslator.ToHtml(dlg.Color);
                _appModel.Background = color;
                Settings.Background = color;
            }
        }
    }
}
