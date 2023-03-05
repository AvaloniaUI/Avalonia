using Avalonia.Controls;
using ControlCatalog.Models;

namespace ControlCatalog.Pages
{
    public partial class LabelsPage : UserControl
    {
        private Person? _person;

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

        public void DoSave()
        {
            
        }
        public void DoCancel()
        {
            CreateDefaultPerson();
        }
    }
}
