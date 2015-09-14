// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using ReactiveUI;

namespace XamlTestApplication
{
    public class MainViewModel : ReactiveObject
    {
        private string _name;

        public MainViewModel()
        {
            Name = "Jos\u00E9 Manuel";
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
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }

        public List<Person> People { get; set; }
    }

    public class Person
    {
        private string _name;

        public Person(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}