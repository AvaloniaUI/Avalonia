using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Data;
using System.Collections.Generic;

namespace ControlCatalog.Pages
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class DataGridPage : UserControl
    {
        List<Person> _data;

        public DataGridPage()
        {
            this.InitializeComponent();

            _data = new List<Person>();
            _data.Add(new Person { FirstName = "Nat", LastName = "Dean" });
            _data.Add(new Person { FirstName = "Sam", LastName = "Doroff" });
            _data.Add(new Person { FirstName = "Kat", LastName = "Ward" });

            var dg = this.FindControl<DataGrid>("dataGrid1");
            dg.IsReadOnly = true;
            //var c1 = new DataGridTextColumn
            //{
            //    Binding = new Binding { Path = "FirstName" },
            //    Header = "First Name"
            //};
            //var c2 = new DataGridTextColumn
            //{
            //    Binding = new Binding { Path = "LastName" },
            //    Header = "Last Name"
            //};
            //dg.Columns.Add(c1);
            //dg.Columns.Add(c2);

            dg.Items = _data;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
