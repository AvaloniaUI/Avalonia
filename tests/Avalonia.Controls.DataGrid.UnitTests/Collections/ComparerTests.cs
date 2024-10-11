using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Logging;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Collections;

public class NoStringTypeComparerTests
{
    public static IEnumerable<object[]> GetComparerForNotStringTypeParameters()
    {
        yield return
        [
            nameof(Item.IntProp1),
            (object item, object value) =>
            {
                (item as Item)!.IntProp1 = (int)value;
            },
            (object item) => (object)(item as Item)!.IntProp1,
            new object[] { 2, 3, 1 },
            new object[] { 1, 2, 3 }
        ];
        yield return
        [
            nameof(Item.IntProp2),
            (object item, object value) =>
            {
                (item as Item)!.IntProp2 = (int?)value;
            },
            (object item) => (object)(item as Item)!.IntProp2,
            new object[] { 2, 3, null, 1 },
            new object[] { null, 1, 2, 3 }
        ];
        yield return
        [
            nameof(Item.DoubleProp1),
            (object item, object value) =>
            {
                (item as Item)!.DoubleProp1 = (double)value;
            },
            (object item) => (object)(item as Item)!.DoubleProp1,
            new object[] { 2.1, 3.1, 1.1 },
            new object[] { 1.1, 2.1, 3.1 }
        ];
        yield return
        [
            nameof(Item.DoubleProp2),
            (object item, object value) =>
            {
                (item as Item)!.DoubleProp2 = (double?)value;
            },
            (object item) => (object)(item as Item)!.DoubleProp2,
            new object[] { 2.1, 3.1, null, 1.1 },
            new object[] { null, 1.1, 2.1, 3.1 }
        ];
        yield return
        [
            nameof(Item.DecimalProp1),
            (object item, object value) =>
            {
                (item as Item)!.DecimalProp1 = (decimal)value;
            },
            (object item) => (object)(item as Item)!.DecimalProp1,
            new object[] { 2.1M, 3.1M, 1.1M },
            new object[] { 1.1M, 2.1M, 3.1M }
        ];
        yield return
        [
            nameof(Item.DecimalProp2),
            (object item, object value) =>
            {
                (item as Item)!.DecimalProp2 = (decimal?)value;
            },
            (object item) => (object)(item as Item)!.DecimalProp2,
            new object[] { 2.1M, 3.1M, null, 1.1M },
            new object[] { null, 1.1M, 2.1M, 3.1M }
        ];
        yield return
        [
            nameof(Item.EnumProp1),
            (object item, object value) =>
            {
                (item as Item)!.EnumProp1 = (LogEventLevel)value;
            },
            (object item) => (object)(item as Item)!.EnumProp1,
            new object[] { LogEventLevel.Information, LogEventLevel.Debug, LogEventLevel.Error },
            new object[] { LogEventLevel.Debug, LogEventLevel.Information, LogEventLevel.Error }
        ];
        yield return
        [
            nameof(Item.EnumProp2),
            (object item, object value) =>
            {
                (item as Item)!.EnumProp2 = (LogEventLevel?)value;
            },
            (object item) => (object)(item as Item)!.EnumProp2,
            new object[]
            {
                LogEventLevel.Information,
                LogEventLevel.Debug,
                null,
                LogEventLevel.Error
            },
            new object[]
            {
                null,
                LogEventLevel.Debug,
                LogEventLevel.Information,
                LogEventLevel.Error
            }
        ];
        yield return
        [
            nameof(Item.CustomProp2),
            (object item, object value) =>
            {
                (item as Item)!.CustomProp2 = (CustomType?)value;
            },
            (object item) => (object)(item as Item)!.CustomProp2,
            new object[]
            {
                new CustomType() { Prop = 2 },
                new CustomType() { Prop = 3 },
                null,
                new CustomType() { Prop = 1 }
            },
            new object[]
            {
                null,
                new CustomType() { Prop = 1 },
                new CustomType() { Prop = 2 },
                new CustomType() { Prop = 3 }
            }
        ];
    }

    [Theory]
    [MemberData(nameof(GetComparerForNotStringTypeParameters))]
    public void GetComparerForNotStringType_Correctly_WhenSorting(
        string pathName,
        Action<object, object> setAction,
        Func<object, object> getAction,
        object[] orignal,
        object[] ordered
    )
    {
        List<Item> items = new();
        for (int i = 0; i < orignal.Length; i++)
        {
            var item = new Item();
            setAction(item, orignal[i]);
            items.Add(item);
        }

        //Ascending
        var sortDescription = DataGridSortDescription.FromPath(
            pathName,
            ListSortDirection.Ascending
        );
        sortDescription.Initialize(typeof(Item));
        var result = sortDescription.OrderBy(items).ToList();

        for (int i = 0; i < ordered.Length; i++)
        {
            Assert.Equal(ordered[i], getAction(result[i]));
        }

        //Descending
        sortDescription = DataGridSortDescription.FromPath(pathName, ListSortDirection.Descending);
        sortDescription.Initialize(typeof(Item));
        result = sortDescription.OrderBy(items).ToList();

        ordered = ordered.Reverse().ToArray();
        for (int i = 0; i < ordered.Length; i++)
        {
            Assert.Equal(ordered[i], getAction(result[i]));
        }
    }

    private class Item
    {
        public int IntProp1 { get; set; }
        public int? IntProp2 { get; set; }
        public double DoubleProp1 { get; set; }
        public double? DoubleProp2 { get; set; }

        public decimal DecimalProp1 { get; set; }
        public decimal? DecimalProp2 { get; set; }

        public LogEventLevel EnumProp1 { get; set; }
        public LogEventLevel? EnumProp2 { get; set; }
        public CustomType? CustomProp2 { get; set; }
    }

    public struct CustomType : IComparable
    {
        public int Prop { get; set; }

        public int CompareTo(object obj)
        {
            if (obj is CustomType other)
            {
                return Prop.CompareTo(other.Prop);
            }
            else
            {
                return 1;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is CustomType other)
            {
                return Prop == other.Prop;
            }
            return false;
        }
    }
}
