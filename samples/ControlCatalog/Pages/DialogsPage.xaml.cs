using Avalonia.Controls;
using Avalonia.Markup.Xaml;
#pragma warning disable 4014

namespace ControlCatalog.Pages
{
    public class DialogsPage : UserControl
    {
        public DialogsPage()
        {
            this.InitializeComponent();
            this.FindControl<Button>("OpenFile").Click += delegate
            {
                new OpenFileDialog()
                {
                    Title = "Open file"
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("SaveFile").Click += delegate
            {
                new SaveFileDialog()
                {
                    Title = "Save file"
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("SelectFolder").Click += delegate
            {
                new OpenFolderDialog()
                {
                    Title = "Select folder"
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("DecoratedWindow").Click += delegate
                {
                    new DecoratedWindow().ShowDialog(GetWindow());
                };
            this.FindControl<Button>("Dialog").Click += delegate
                {
                    new MainWindow().ShowDialog(GetWindow());
                };


        }

        Window GetWindow() => (Window)this.VisualRoot;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
