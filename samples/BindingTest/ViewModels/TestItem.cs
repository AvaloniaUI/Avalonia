using ReactiveUI;

namespace BindingTest.ViewModels
{
    public class TestItem : ReactiveObject
    {
        private string stringValue = "String Value";

        public string StringValue
        {
            get { return stringValue; }
            set { this.RaiseAndSetIfChanged(ref this.stringValue, value); }
        }
    }
}
