namespace BingSearchApp.ViewModels
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using ReactiveUI;

    public class ViewModel : ReactiveObject
    {
        private readonly IMessageBoxService messageBoxService;
        private string name;

        private DateTime dateTime;

        private int number;
        private ReactiveCommand<object> showMessageCommand;

        public ViewModel(IMessageBoxService messageBoxService)
        {
            this.messageBoxService = messageBoxService;
            Name = "Yo, man!";
            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => DateTime = DateTime.Now);
            Number = 10;
            Person = new Person { Name = "Name of person" };
            ShowMessageCommand = ReactiveCommand.Create();
            ShowMessageCommand.Subscribe(OnShowMessage);
            People = new List<Person>
            {
                new Person {Name = "Johnny", Surname = "Bravo"},
                new Person {Name = "Billy", Surname = "The Kid"},
                new Person {Name = "Tommy", Surname = "The Who's"}
            };
        }

        private void OnShowMessage(object o)
        {
            messageBoxService.Show("Greetings!");
        }

        public ReactiveCommand<object> ShowMessageCommand
        {
            get { return showMessageCommand; }
            set { showMessageCommand = value; }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref name, value);
            }
        }

        public DateTime DateTime
        {
            get
            {
                return dateTime;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref dateTime, value);
            }
        }

        public int Number
        {
            get
            {
                return number;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref number, value);
            }
        }

        public IEnumerable People { get; set; }

        public Person Person { get; set; }
    }
}