namespace BingSearchApp
{
    using System.Windows.Forms;
    using ViewModels;

    internal class MessageBoxService : IMessageBoxService
    {
        public void Show(string message)
        {
            MessageBox.Show(message);
        }
    }
}