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
