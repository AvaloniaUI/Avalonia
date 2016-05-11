using ReactiveUI;

namespace BindingTest.ViewModels
{
    public class TestItem : ReactiveObject
    {
        private string _stringValue = "String Value";
        private string _detail;

        public string StringValue
        {
            get { return _stringValue; }
            set { this.RaiseAndSetIfChanged(ref this._stringValue, value); }
        }

        public string Detail
        {
            get { return _detail; }
            set { this.RaiseAndSetIfChanged(ref this._detail, value); }
        }
    }
}
