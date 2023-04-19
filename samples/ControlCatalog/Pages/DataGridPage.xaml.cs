using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.Models;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public class DataGridPage : UserControl
    {
        public DataGridPage()
        {
            this.InitializeComponent();

            var dataGridSortDescription = DataGridSortDescription.FromPath(nameof(Country.Region), ListSortDirection.Ascending, new ReversedStringComparer());
            var collectionView1 = new DataGridCollectionView(Countries.All);
            collectionView1.SortDescriptions.Add(dataGridSortDescription);
            var dg1 = this.Get<DataGrid>("dataGrid1");
            dg1.IsReadOnly = true;
            dg1.LoadingRow += Dg1_LoadingRow;
            dg1.Sorting += (s, a) =>
            {
                var binding = (a.Column as DataGridBoundColumn)?.Binding as Binding;

                if (binding?.Path is string property
                    && property == dataGridSortDescription.PropertyPath
                    && !collectionView1.SortDescriptions.Contains(dataGridSortDescription))
                {
                    collectionView1.SortDescriptions.Add(dataGridSortDescription);
                }
            };
            dg1.ItemsSource = collectionView1;

            var dg2 = this.Get<DataGrid>("dataGridGrouping");
            dg2.IsReadOnly = true;

            var collectionView2 = new DataGridCollectionView(Countries.All);
            collectionView2.GroupDescriptions.Add(new DataGridPathGroupDescription("Region"));

            dg2.ItemsSource = collectionView2;

            var dg3 = this.Get<DataGrid>("dataGridEdit");
            dg3.IsReadOnly = false;

            var list = new ObservableCollection<Person>
            {
                new Person { FirstName = "John", LastName = "Doe" , Age = 30},
                new Person { FirstName = "Elizabeth", LastName = "Thomas", IsBanned = true , Age = 40 },
                new Person { FirstName = "Zack", LastName = "Ward" , Age = 50 }
            };
            DataGrid3Source = list;

            var addButton = this.Get<Button>("btnAdd");
            addButton.Click += (a, b) => list.Add(new Person());

            DataContext = this;
        }

        public IEnumerable<Person> DataGrid3Source { get; }
        
        private void Dg1_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class ReversedStringComparer : IComparer<object>, IComparer
        {
            public int Compare(object? x, object? y)
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

        private void NumericUpDown_OnTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            // We want to focus the TextBox of the NumericUpDown. To do so we search for this control when the template
            // is applied, but we postpone the action until the control is actually loaded. 
            if (e.NameScope.Find<TextBox>("PART_TextBox") is {} textBox)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }, DispatcherPriority.Loaded);
            }
        }
    }
}
