using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public partial class CheckBoxPage : UserControl
    {
        private TextBlock myTb;
        private int count;

        public CheckBoxPage()
        {
            this.InitializeComponent();

            myTb = this.FindControl<TextBlock>("myTb");

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            myTb.Text = e.GetPosition(this).ToString() + ", " + count++;
        }
    }
}
