#nullable enable

using System.Collections.Generic;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities;

public class InlineDictionaryTests
{
    [Fact]
    public void Set_Twice_With_Single_Item_Works()
    {
        var dic = new InlineDictionary<string, int>();
        dic["foo"] = 1;
        Assert.Equal(1, dic["foo"]);

        dic["foo"] = 2;
        Assert.Equal(2, dic["foo"]);
    }

    [Fact]
    public void Enumeration_After_Add_With_Internal_Array_Works()
    {
        var dic = new InlineDictionary<string, int>();
        dic.Add("foo", 1);
        dic.Add("bar", 2);
        dic.Add("baz", 3);

        Assert.Equal(
            new[] {
                new KeyValuePair<string, int>("foo", 1),
                new KeyValuePair<string, int>("bar", 2),
                new KeyValuePair<string, int>("baz", 3)
            },
            dic);
    }

    [Fact]
    public void Enumeration_After_Remove_With_Internal_Array_Works()
    {
        var dic = new InlineDictionary<string, int>();
        dic.Add("foo", 1);
        dic.Add("bar", 2);
        dic.Add("baz", 3);

        Assert.Equal(
            new[] {
                new KeyValuePair<string, int>("foo", 1),
                new KeyValuePair<string, int>("bar", 2),
                new KeyValuePair<string, int>("baz", 3)
            },
            dic);

        dic.Remove("bar");

        Assert.Equal(
            new[] {
                new KeyValuePair<string, int>("foo", 1),
                new KeyValuePair<string, int>("baz", 3)
            },
            dic);
    }
}
