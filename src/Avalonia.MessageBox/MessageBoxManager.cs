using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;

namespace Avalonia.MessageBox
{
    public static class MessageBoxManager
    {
        public static async Task<MessageBoxButton> Show(string message, string caption,
            MessageBoxButton buttons = MessageBoxButton.OK, Bitmap messageBoxIcon = null, Window? ownerWindow = null)
        {
            return await Create(message, caption, buttons, messageBoxIcon, ownerWindow);
        }

        private static async Task<MessageBoxButton> Create(string message, string caption,
            MessageBoxButton buttons = MessageBoxButton.OK, Bitmap messageBoxIcon = null, Window? ownerWindow = null)
        {
            var dialog = new MessageBoxDialog();
            dialog.Setup(message, caption, buttons, messageBoxIcon, ownerWindow ?? GetMainWindow);
            await dialog.ShowDialog(ownerWindow ?? GetMainWindow);
            return dialog.Result;
        }

        private static Window GetMainWindow =>
            ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
    }

}
