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
                new DecoratedWindow().Show();
            };
            this.FindControl<Button>("ChildWindow").Click += delegate
            {

                var window = new Window
                {
                    Height = 200,
                    Width = 200,
                    Content = new Button{ Content = "Test"}
                };

                window.ShowDialog();

            };
        }

        Window GetWindow() => this.FindControl<CheckBox>("IsModal").IsChecked.Value ? (Window)this.VisualRoot : null;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
