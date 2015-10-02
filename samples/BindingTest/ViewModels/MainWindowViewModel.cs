using ReactiveUI;

namespace BindingTest.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _simpleBinding = "Simple Binding";

        public string SimpleBinding
        {
            get { return _simpleBinding; }
            set { this.RaiseAndSetIfChanged(ref _simpleBinding, value); }
        }
    }
}
