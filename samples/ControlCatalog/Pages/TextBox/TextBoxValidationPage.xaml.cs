using Avalonia.Controls;
using ControlCatalog.Models;

namespace ControlCatalog.Pages
{
    public partial class TextBoxValidationPage : UserControl
    {
        public TextBoxValidationPage()
        {
            InitializeComponent();

            DataContext = new Person { FirstName = "John", LastName = "Doe" };
        }
    }
}
