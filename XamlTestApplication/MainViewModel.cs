namespace XamlTestApplication
{
    using System.Collections.Generic;
    using ReactiveUI;

    public class MainViewModel : ReactiveObject
    {
        private string name;

        public MainViewModel()
        {
            Name = "José Manuel";
            People = new List<Person>
            {
                new Person("a little bit of Monica in my life"),
                new Person("a little bit of Erica by my side"),
                new Person("a little bit of Rita is all I need"),
                new Person("a little bit of Tina is what I see"),
                new Person("a little bit of Sandra in the sun"),
                new Person("a little bit of Mary all night long"),
                new Person("a little bit of Jessica here I am"),
            };
        }

        public string Name
        {
            get { return name; }
            set { this.RaiseAndSetIfChanged(ref name, value); }
        }

        public List<Person> People { get; set; }
    }

    public class Person
    {
        private string name;

        public Person(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}