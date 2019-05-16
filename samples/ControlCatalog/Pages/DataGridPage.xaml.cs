using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.Models;
using Avalonia.Collections;

namespace ControlCatalog.Pages
{
    public class DataGridPage : UserControl
    {
        public DataGridPage()
        {
            this.InitializeComponent();
            var dg1 = this.FindControl<DataGrid>("dataGrid1");
            dg1.IsReadOnly = true;

            var collectionView1 = new DataGridCollectionView(Countries.All);
            //collectionView.GroupDescriptions.Add(new PathGroupDescription("Region"));

            dg1.Items = collectionView1;

            var dg2 = this.FindControl<DataGrid>("dataGridGrouping");
            dg2.IsReadOnly = true;

            var collectionView2 = new DataGridCollectionView(Countries.All);
            collectionView2.GroupDescriptions.Add(new DataGridPathGroupDescription("Region"));

            dg2.Items = collectionView2;

            var dg3 = this.FindControl<DataGrid>("dataGridEdit");
            dg3.IsReadOnly = false;

            var items = new List<Person>
            {
                new Person { FirstName = "John", LastName = "Doe" },
                new Person { FirstName = "Elizabeth", LastName = "Thomas" },
                new Person { FirstName = "Zack", LastName = "Ward" }
            };
            var collectionView3 = new DataGridCollectionView(items);

            dg3.Items = collectionView3;

            var addButton = this.FindControl<Button>("btnAdd");
            addButton.Click += (a, b) => collectionView3.AddNew();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
