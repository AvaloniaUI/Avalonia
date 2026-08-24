using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities;

public class SafeEnumerableAvaloniaListTests
{
    [Fact]
    public void List_Is_Not_Copied_Outside_Enumeration()
    {
        var target = new SafeEnumerableAvaloniaList<string>();
        var inner = target.InnerForTests;

        target.Add("foo");
        target.Add("bar");
        target.Remove("foo");
        target.Insert(0, "baz");
        target[0] = "qux";
        target.Move(0, 1);
        target.Clear();

        Assert.Same(inner, target.InnerForTests);
    }

    [Fact]
    public void List_Is_Copied_When_Mutated_During_Enumeration()
    {
        var target = new SafeEnumerableAvaloniaList<string>();
        var inner = target.InnerForTests;

        target.Add("foo");

        foreach (var item in target)
        {
            Assert.Same(inner, target.InnerForTests);
            target.Add("bar");
            Assert.NotSame(inner, target.InnerForTests);
            Assert.Equal("foo", item);
        }

        Assert.Equal(["foo", "bar"], target);
    }

    [Fact]
    public void Enumerator_Iterates_Snapshot_Taken_At_Creation()
    {
        var target = new SafeEnumerableAvaloniaList<string> { "foo", "bar", "baz" };
        var seen = new List<string>();

        foreach (var item in target)
        {
            seen.Add(item);
            target.Remove(item);
        }

        Assert.Equal(["foo", "bar", "baz"], seen);
        Assert.Empty(target);
    }

    [Fact]
    public void List_Is_Not_Copied_After_Enumeration()
    {
        var target = new SafeEnumerableAvaloniaList<string>();
        var inner = target.InnerForTests;

        target.Add("foo");

        foreach (var item in target)
        {
            target.Add("bar");
            Assert.NotSame(inner, target.InnerForTests);
            inner = target.InnerForTests;
            Assert.Equal("foo", item);
        }

        target.Add("baz");
        Assert.Same(inner, target.InnerForTests);
    }

    [Fact]
    public void List_Is_Copied_Only_Once_During_Enumeration()
    {
        var target = new SafeEnumerableAvaloniaList<string>();
        var inner = target.InnerForTests;

        target.Add("foo");

        foreach (var item in target)
        {
            target.Add("bar");
            Assert.NotSame(inner, target.InnerForTests);
            inner = target.InnerForTests;
            target.Add("baz");
            Assert.Same(inner, target.InnerForTests);
        }
    }

    [Fact]
    public void List_Is_Copied_During_Nested_Enumerations()
    {
        var target = new SafeEnumerableAvaloniaList<string>();
        var initialInner = target.InnerForTests;
        var firstItems = new List<string>();
        var secondItems = new List<string>();

        target.Add("foo");

        foreach (var i in target)
        {
            target.Add("bar");

            var firstInner = target.InnerForTests;
            Assert.NotSame(initialInner, firstInner);

            foreach (var j in target)
            {
                target.Add("baz");

                var secondInner = target.InnerForTests;
                Assert.NotSame(firstInner, secondInner);

                secondItems.Add(j);
            }

            firstItems.Add(i);
        }

        Assert.Equal(["foo"], firstItems);
        Assert.Equal(["foo", "bar"], secondItems);
        Assert.Equal(["foo", "bar", "baz", "baz"], target);

        var finalInner = target.InnerForTests;
        target.Add("final");
        Assert.Same(finalInner, target.InnerForTests);
    }

    [Fact]
    public void Enumeration_Through_Interface_Is_Safe()
    {
        var target = new SafeEnumerableAvaloniaList<string> { "foo", "bar" };
        var seen = new List<string>();

        foreach (var item in (IEnumerable<string>)target)
        {
            seen.Add(item);
            target.Add("baz");
        }

        Assert.Equal(["foo", "bar"], seen);
        Assert.Equal(["foo", "bar", "baz", "baz"], target);
    }

    [Fact]
    public void CollectionChanged_Is_Raised_For_Mutations_After_Copy()
    {
        var target = new SafeEnumerableAvaloniaList<string> { "foo" };
        var events = new List<NotifyCollectionChangedEventArgs>();

        target.CollectionChanged += (_, e) => events.Add(e);

        foreach (var item in target)
        {
            target.Add("bar");
        }

        target.Add("baz");

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(NotifyCollectionChangedAction.Add, e.Action));
        Assert.Equal("bar", events[0].NewItems!.Cast<string>().Single());
        Assert.Equal("baz", events[1].NewItems!.Cast<string>().Single());
    }

    [Fact]
    public void PropertyChanged_Is_Raised_For_Count_After_Copy()
    {
        var target = new SafeEnumerableAvaloniaList<string> { "foo" };
        var countChanged = 0;

        target.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(target.Count))
                ++countChanged;
        };

        foreach (var i in target)
            target.Add("bar");

        target.Add("baz");

        Assert.Equal(2, countChanged);
    }

    [Fact]
    public void Validator_Is_Invoked_After_Copy()
    {
        var target = new SafeEnumerableAvaloniaList<string> { "foo" };
        var validated = new List<string>();

        target.Validate = validated.Add;

        foreach (var item in target)
            target.Add("bar");

        target.Add("baz");

        Assert.Equal(["bar", "baz"], validated);
    }
}
