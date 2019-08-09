using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.Platform;

namespace Avalonia.Dialogs
{
    public static class ManagedFileDialogExtensions
    {
        class ManagedSystemDialogImpl<T> : ISystemDialogImpl where T : Window, new()
        {
            async Task<string[]> Show(SystemDialog d, IWindowImpl parent)
            {
                var model = new ManagedFileChooserViewModel((FileSystemDialog)d);

                var dialog = new T
                {
                    Content = new ManagedFileChooser(),
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

            public async Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
            {
                return await Show(dialog, parent);
            }

            public async Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
            {
                return (await Show(dialog, parent))?.FirstOrDefault();
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
    }
}
