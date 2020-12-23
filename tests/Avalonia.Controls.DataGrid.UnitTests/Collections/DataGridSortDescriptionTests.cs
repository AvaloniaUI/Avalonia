using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Xunit;

namespace Avalonia.Controls.DataGrid.UnitTests.Collections
{

    public class DataGridSortDescriptionTests
    {
        [Fact]
        public void OrderBy_Orders_Correctly_When_Ascending()
        {
            var items = new[]
            {
                new Item("b", "b"),
                new Item("a", "a"),
                new Item("c", "c"),
            };
            var expectedResult = items.OrderBy(i => i.Prop1).ToList();
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop1), ListSortDirection.Ascending);
            
            sortDescription.Initialize(typeof(Item));
            var result = sortDescription.OrderBy(items).ToList();
            
            Assert.Equal(expectedResult, result);
        }
        
        [Fact]
        public void OrderBy_Orders_Correctly_When_Descending()
        {
            var items = new[]
            {
                new Item("b", "b"),
                new Item("a", "a"),
                new Item("c", "c"),
            };
            var expectedResult = items.OrderByDescending(i => i.Prop1).ToList();
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop1), ListSortDirection.Descending);
            
            sortDescription.Initialize(typeof(Item));
            var result = sortDescription.OrderBy(items).ToList();
            
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void ThenBy_Orders_Correctly_When_Ascending()
        {
            // Casting nonsense below because IOrderedEnumerable<T> isn't covariant in full framework and we need an
            // object of type IOrderedEnumerable<object> for DataGridSortDescription.ThenBy
            var items = new[]
            {
                (object)new Item("a", "b"),
                        new Item("a", "a"),
                        new Item("a", "c"), 
            }.OrderBy(i => ((Item)i).Prop1);
            var expectedResult = new[]
            {
                new Item("a", "a"),
                new Item("a", "b"),
                new Item("a", "c"),
            };
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop2), ListSortDirection.Ascending);
            
            sortDescription.Initialize(typeof(Item));
            var result = sortDescription.ThenBy(items).ToList();
            
            Assert.Equal(expectedResult, result);
        }
        
        [Fact]
        public void ThenBy_Orders_Correctly_When_Descending()
        {
            // Casting nonsense below because IOrderedEnumerable<T> isn't covariant in full framework and we need an
            // object of type IOrderedEnumerable<object> for DataGridSortDescription.ThenBy
            var items = new[]
            {
                (object)new Item("a", "b"),
                        new Item("a", "a"),
                        new Item("a", "c"), 
            }.OrderBy(i => ((Item)i).Prop1);
            var expectedResult = new[]
            {
                new Item("a", "c"),
                new Item("a", "b"),
                new Item("a", "a"),
            };
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop2), ListSortDirection.Descending);
            
            sortDescription.Initialize(typeof(Item));
            var result = sortDescription.ThenBy(items).ToList();
            
            Assert.Equal(expectedResult, result);
        }

        private class Item : IEquatable<Item>
        {
            public Item(string prop1, string prop2)
            {
                Prop1 = prop1;
                Prop2 = prop2;
            }

            public string Prop1 { get; }
            public string Prop2 { get; }

            public bool Equals(Item other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Prop1 == other.Prop1 && Prop2 == other.Prop2;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Item) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Prop1 != null ? Prop1.GetHashCode() : 0) * 397) ^ (Prop2 != null ? Prop2.GetHashCode() : 0);
                }
            }
        }
    }
}
