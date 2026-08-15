using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ControlCatalog.Pages
{
    public partial class GesturePinchRotationPage : UserControl
    {
        private bool _isInit;

        public GesturePinchRotationPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_isInit)
            {
                return;
            }

            _isInit = true;

            RotationGesture.AddHandler(InputElement.PinchEvent, (s, e) =>
            {
                AngleSlider.Value = e.Angle;
            });
        }
    }
}
