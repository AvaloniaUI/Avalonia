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
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop1), @descending: false);
            
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
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop1), @descending: true);
            
            sortDescription.Initialize(typeof(Item));
            var result = sortDescription.OrderBy(items).ToList();
            
            Assert.Equal(expectedResult, result);
        }
        
        [Fact]
        public void ThenBy_Orders_Correctly_When_Ascending()
        {
            var items = new[]
            {
                new Item("a", "b"),
                new Item("a", "a"),
                new Item("a", "c"), 
            }.OrderBy(i => i.Prop1);
            var expectedResult = items.ThenBy(i => i.Prop2).ToList();
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop2), @descending: false);
            
            sortDescription.Initialize(typeof(Item));
            var result = sortDescription.ThenBy((IOrderedEnumerable<object>)items).ToList();
            
            Assert.Equal(expectedResult, result);
        }
        
        [Fact]
        public void ThenBy_Orders_Correctly_When_Descending()
        {
            var items = new[]
            {
                new Item("a", "b"),
                new Item("a", "a"),
                new Item("a", "c"), 
            }.OrderBy(i => i.Prop1);
            var expectedResult = items.ThenByDescending(i => i.Prop2).ToList();
            var sortDescription = DataGridSortDescription.FromPath(nameof(Item.Prop2), @descending: true);
            
            sortDescription.Initialize(typeof(Item));
            var result = sortDescription.ThenBy((IOrderedEnumerable<object>)items).ToList();
            
            Assert.Equal(expectedResult, result);
        }
        
        private class Item
        {
            public Item(string prop1, string prop2)
            {
                Prop1 = prop1;
                Prop2 = prop2;
            }

            public string Prop1 { get; }
            public string Prop2 { get; }
        }
    }
}
