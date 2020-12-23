using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.Models;

namespace ControlCatalog.Pages
{
    public class LabelsPage : UserControl
    {
        private Person _person;

        public LabelsPage()
        {
            CreateDefaultPerson();
            this.InitializeComponent();
        }

        private void CreateDefaultPerson()
        {
            DataContext = _person = new Person
            {
                FirstName = "John",
                LastName = "Doe",
                IsBanned = true,
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void DoSave()
        {
            
        }
        public void DoCancel()
        {
            CreateDefaultPerson();
        }
    }
}
