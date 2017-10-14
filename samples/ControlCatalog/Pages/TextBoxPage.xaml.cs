using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class TestViewModel : ReactiveObject
    {
        private string testText = "Editable TextBlock";

        public string TestText
        {
            get { return testText; }
            set { this.RaiseAndSetIfChanged(ref testText, value); }
        }
    }

    public class TextBoxPage : UserControl
    {
        public TextBoxPage()
        {
            this.InitializeComponent();

            DataContext = new TestViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
