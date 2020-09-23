namespace Avalonia.Diagnostics.ViewModels
{
    class EditFavoritePropertiesViewModel : ViewModelBase
    {
        private string _name;
        private string[] _properies;

        public string Name
        {
            get => _name;
            set
            {
                RaiseAndSetIfChanged(ref _name, value);
            }
        }

        public string[] Properies 
        { 
            get => _properies;
            set
            {
                RaiseAndSetIfChanged(ref _properies, value);
            }
        }
    }
}
