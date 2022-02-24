using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avalonia.Dialogs
{
    public static class ManagedFileDialogExtensions
    {
        private class ManagedSystemDialogImpl<T> : ISystemDialogImpl where T : Window, new()
        {
            async Task<string[]> Show(SystemDialog d, Window parent, ManagedFileDialogOptions options = null)
            {
                var model = new ManagedFileChooserViewModel((FileSystemDialog)d,
                    options ?? new ManagedFileDialogOptions());

                var dialog = new T
                {
                    Content = new ManagedFileChooser(),
                    Title = d.Title,
                    DataContext = model
                };

                dialog.Closed += delegate { model.Cancel(); };

                string[] result = null;
                
                model.CompleteRequested += items =>
                {
                    result = items;
                    dialog.Close();
                };

                model.OverwritePrompt += async (filename) =>
                {
                    Window overwritePromptDialog = new Window()
                    {
                        Title = "Confirm Save As",
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Padding = new Thickness(10),
                        MinWidth = 270
                    };

                    string name = Path.GetFileName(filename);

                    var panel = new DockPanel()
                    {
                        HorizontalAlignment = Layout.HorizontalAlignment.Stretch
                    };

                    var label = new Label()
                    {
                        Content = $"{name} already exists.\nDo you want to replace it?"
                    };

                    panel.Children.Add(label);
                    DockPanel.SetDock(label, Dock.Top);

                    var buttonPanel = new StackPanel()
                    {
                        HorizontalAlignment = Layout.HorizontalAlignment.Right,
                        Orientation = Layout.Orientation.Horizontal,
                        Spacing = 10
                    };

                    var button = new Button()
                    {
                        Content = "Yes",
                        HorizontalAlignment = Layout.HorizontalAlignment.Right
                    };

                    button.Click += (sender, args) =>
                    {
                        result = new string[1] { filename };
                        overwritePromptDialog.Close();
                        dialog.Close();
                    };

                    buttonPanel.Children.Add(button);

                    button = new Button()
                    {
                        Content = "No",
                        HorizontalAlignment = Layout.HorizontalAlignment.Right
                    };

                    button.Click += (sender, args) =>
                    {
                        overwritePromptDialog.Close();
                    };

                    buttonPanel.Children.Add(button);

                    panel.Children.Add(buttonPanel);
                    DockPanel.SetDock(buttonPanel, Dock.Bottom);

                    overwritePromptDialog.Content = panel;

                    await overwritePromptDialog.ShowDialog(dialog);
                };

                model.CancelRequested += dialog.Close;

                await dialog.ShowDialog<object>(parent);
                return result;
            }

            public async Task<string[]> ShowFileDialogAsync(FileDialog dialog, Window parent)
            {
                return await Show(dialog, parent);
            }

            public async Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
            {
                return (await Show(dialog, parent))?.FirstOrDefault();
            }
            
            public async Task<string[]> ShowFileDialogAsync(FileDialog dialog, Window parent, ManagedFileDialogOptions options)
            {
                return await Show(dialog, parent, options);
            }
        }

        public static TAppBuilder UseManagedSystemDialogs<TAppBuilder>(this TAppBuilder builder)
            where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        {
            builder.AfterSetup(_ =>
                AvaloniaLocator.CurrentMutable.Bind<ISystemDialogImpl>().ToSingleton<ManagedSystemDialogImpl<Window>>());
            return builder;
        }

        public static TAppBuilder UseManagedSystemDialogs<TAppBuilder, TWindow>(this TAppBuilder builder)
            where TAppBuilder : AppBuilderBase<TAppBuilder>, new() where TWindow : Window, new()
        {
            builder.AfterSetup(_ =>
                AvaloniaLocator.CurrentMutable.Bind<ISystemDialogImpl>().ToSingleton<ManagedSystemDialogImpl<TWindow>>());
            return builder;
        }

        public static Task<string[]> ShowManagedAsync(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions options = null) => ShowManagedAsync<Window>(dialog, parent, options);
        
        public static Task<string[]> ShowManagedAsync<TWindow>(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions options = null) where TWindow : Window, new()
        {
            return new ManagedSystemDialogImpl<TWindow>().ShowFileDialogAsync(dialog, parent, options);
        }
    }
}
