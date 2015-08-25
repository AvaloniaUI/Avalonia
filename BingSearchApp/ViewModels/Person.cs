namespace BingSearchApp.ViewModels
{
    using ReactiveUI;

    public class Person : ReactiveObject
    {
        private string name;
        private string surname;

        public string Name
        {
            get { return name; }
            set { this.RaiseAndSetIfChanged(ref name, value); }
        }

        public string Surname
        {
            get { return surname; }
            set
            {
                
                this.RaiseAndSetIfChanged(ref surname, value);
            }
        }
    }
}