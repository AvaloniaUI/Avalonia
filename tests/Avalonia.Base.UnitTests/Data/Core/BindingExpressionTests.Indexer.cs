using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void Should_Get_Array_Value()
    {
        var data = new { Foo = new[] { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[1]);

        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_MultiDimensional_Array_Value()
    {
        var data = new { Foo = new[,] { { "foo", "bar" }, { "baz", "qux" } } };
        var target = CreateTargetWithSource(data, o => o.Foo[1, 1]);

        Assert.Equal("qux", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Value_For_String_Indexer()
    {
        var data = new { Foo = new Dictionary<string, string> { { "foo", "bar" }, { "baz", "qux" } } };
        var target = CreateTargetWithSource(data, o => o.Foo["foo"]);

        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Value_For_Non_String_Indexer()
    {
        var data = new { Foo = new Dictionary<double, string> { { 1.0, "bar" }, { 2.0, "qux" } } };
        var target = CreateTargetWithSource(data, o => o.Foo[1.0]);

        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Array_Out_Of_Bounds_Should_Return_UnsetValue()
    {
        var data = new { Foo = new[] { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[2]);

        Assert.False(target.IsSet(TargetClass.StringProperty));

        GC.KeepAlive(data);
    }

    [Fact]
    public void List_Out_Of_Bounds_Should_Return_UnsetValue()
    {
        var data = new { Foo = new List<string> { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[2]);

        Assert.False(target.IsSet(TargetClass.StringProperty));

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_List_Value()
    {
        var data = new { Foo = new List<string> { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[1]);

        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Track_INCC_Add()
    {
        var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[2]);

        Assert.False(target.IsSet(TargetClass.StringProperty));

        data.Foo.Add("baz");

        Assert.Equal("baz", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Track_INCC_Remove()
    {
        var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[0]);

        Assert.Equal("foo", target.String);

        data.Foo.RemoveAt(0);

        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Track_INCC_Replace()
    {
        var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[1]);

        Assert.Equal("bar", target.String);

        data.Foo[1] = "baz";

        Assert.Equal("baz", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Track_INCC_Move()
    {
        // Using ObservableCollection here because AvaloniaList does not yet have a Move
        // method, but even if it did we need to test with ObservableCollection as well
        // as AvaloniaList as it implements PropertyChanged as an explicit interface event.
        var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[1]);

        Assert.Equal("bar", target.String);

        data.Foo.Move(0, 1);

        Assert.Equal("foo", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Track_INCC_Reset()
    {
        var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
        var target = CreateTargetWithSource(data, o => o.Foo[1]);
        var result = new List<object?>();

        Assert.Equal("bar", target.String);

        data.Foo.Clear();

        Assert.Equal(null, target.String);
    }

    [Fact]
    public void Should_Track_NonIntegerIndexer()
    {
        var data = new { Foo = new NonIntegerIndexer() };
        data.Foo["foo"] = "bar";
        data.Foo["baz"] = "qux";

        var target = CreateTargetWithSource(data, o => o.Foo["foo"]);

        Assert.Equal("bar", target.String);

        data.Foo["foo"] = "bar2";

        // Forces WeakEvent compact
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("bar2", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_SetArrayIndex()
    {
        var data = new { Foo = new[] { "foo", "bar" } };
        var target = CreateTargetWithSource(
            data,
            o => o.Foo[1],
            mode: BindingMode.TwoWay);

        target.String = "baz";

        Assert.Equal("baz", data.Foo[1]);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Set_ExistingDictionaryEntry()
    {
        var data = new
        {
            Foo = new Dictionary<string, int>
            {
                {"foo", 1 }
            }
        };

        var target = CreateTargetWithSource(
            data, 
            o => o.Foo["foo"],
            mode: BindingMode.TwoWay);

        target.Int = 4;

        Assert.Equal(4, data.Foo["foo"]);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Add_NewDictionaryEntry()
    {
        var data = new
        {
            Foo = new Dictionary<string, int>
            {
                {"foo", 1 }
            }
        };

        var target = CreateTargetWithSource(
            data, 
            o => o.Foo["bar"],
            mode: BindingMode.TwoWay);

        target.Int = 4;

        Assert.Equal(4, data.Foo["bar"]);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Set_NonIntegerIndexer()
    {
        var data = new { Foo = new NonIntegerIndexer() };
        data.Foo["foo"] = "bar";
        data.Foo["baz"] = "qux";

        var target = CreateTargetWithSource(
            data, 
            o => o.Foo["foo"],
            mode: BindingMode.TwoWay);

        target.String = "bar2";

        Assert.Equal("bar2", data.Foo["foo"]);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Indexer_Only_Binding_Works()
    {
        var data = new[] { 1, 2, 3 };
        var target = CreateTargetWithSource(data, o => o[1]);

        Assert.Equal(data[1], target.Int);
    }

    private class NonIntegerIndexer : NotifyingBase
    {
        private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                return _storage[key];
            }
            set
            {
                _storage[key] = value;
                RaisePropertyChanged(CommonPropertyNames.IndexerName);
            }
        }
    }
}
