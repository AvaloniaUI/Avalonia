using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.Models;
using Avalonia.Collections;
using Avalonia.Data;

namespace ControlCatalog.Pages
{
    public class DataGridPage : UserControl
    {
        public DataGridPage()
        {
            this.InitializeComponent();

            var dataGridSortDescription = DataGridSortDescription.FromPath(nameof(Country.Region), ListSortDirection.Ascending, new ReversedStringComparer());
            var colelctionView1 = new DataGridCollectionView(Countries.All);
            colelctionView1.SortDescriptions.Add(dataGridSortDescription);
            var dg1 = this.FindControl<DataGrid>("dataGrid1");
            dg1.IsReadOnly = true;
            dg1.LoadingRow += Dg1_LoadingRow;
            dg1.Sorting += (s, a) =>
            {
                var property = ((a.Column as DataGridBoundColumn)?.Binding as Binding).Path;
                if (property == dataGridSortDescription.PropertyPath
                    && !colelctionView1.SortDescriptions.Contains(dataGridSortDescription))
                {
                    colelctionView1.SortDescriptions.Add(dataGridSortDescription);
                }
            };
            dg1.Items = colelctionView1;

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
                new Person { FirstName = "Elizabeth", LastName = "Thomas", IsBanned = true },
                new Person { FirstName = "Zack", LastName = "Ward" }
            };
            var collectionView3 = new DataGridCollectionView(items);

            dg3.Items = collectionView3;

            var addButton = this.FindControl<Button>("btnAdd");
            addButton.Click += (a, b) => collectionView3.AddNew();
        }

        private void Dg1_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class ReversedStringComparer : IComparer<object>, IComparer
        {
            public int Compare(object x, object y)
            {
                if (x is string left && y is string right)
                {
                    var reversedLeft = new string(left.Reverse().ToArray());
                    var reversedRight = new string(right.Reverse().ToArray());
                    return reversedLeft.CompareTo(reversedRight);
                }

                return Comparer.Default.Compare(x, y);
            }
        }
    }
}
