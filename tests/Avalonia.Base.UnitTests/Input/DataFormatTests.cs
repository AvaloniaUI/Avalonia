using System;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public sealed class DataFormatTests
{
    [Fact]
    public void CreateInProcessFormat_Returns_Format_With_InProcess_Kind()
    {
        var format = DataFormat.CreateInProcessFormat<string>("my-format");

        Assert.Equal(DataFormatKind.InProcess, format.Kind);
    }

    [Fact]
    public void CreateInProcessFormat_Returns_Format_With_Correct_Identifier()
    {
        var format = DataFormat.CreateInProcessFormat<string>("my-format");

        Assert.Equal("my-format", format.Identifier);
    }

    [Fact]
    public void CreateInProcessFormat_Throws_On_Null_Identifier()
    {
        Assert.Throws<ArgumentNullException>(() => DataFormat.CreateInProcessFormat<string>(null!));
    }

    [Fact]
    public void CreateInProcessFormat_Throws_On_Empty_Identifier()
    {
        Assert.Throws<ArgumentException>(() => DataFormat.CreateInProcessFormat<string>(string.Empty));
    }

    [Fact]
    public void CreateInProcessFormat_Allows_Non_ASCII_Identifiers()
    {
        var format = DataFormat.CreateInProcessFormat<string>("日本語フォーマット");

        Assert.Equal("日本語フォーマット", format.Identifier);
    }

    [Fact]
    public void ToSystemName_Throws_For_InProcess()
    {
        var format = DataFormat.CreateInProcessFormat<string>("test");

        Assert.Throws<InvalidOperationException>(() => format.ToSystemName("prefix."));
    }

    [Fact]
    public void InProcess_Format_Equality_Same_Identifier()
    {
        var format1 = DataFormat.CreateInProcessFormat<string>("my-format");
        var format2 = DataFormat.CreateInProcessFormat<string>("my-format");

        Assert.Equal(format1, format2);
        Assert.True(format1 == format2);
    }

    [Fact]
    public void InProcess_Format_Inequality_Different_Identifier()
    {
        var format1 = DataFormat.CreateInProcessFormat<string>("format-a");
        var format2 = DataFormat.CreateInProcessFormat<string>("format-b");

        Assert.NotEqual(format1, format2);
        Assert.True(format1 != format2);
    }

    [Fact]
    public void InProcess_Format_Inequality_Different_Kind_Same_Identifier()
    {
        var inProcess = DataFormat.CreateInProcessFormat<string>("test-format");
        var application = DataFormat.CreateStringApplicationFormat("test-format");

        Assert.NotEqual<DataFormat>(inProcess, application);
    }

    [Fact]
    public void TryGetRaw_With_Mismatched_Format_Returns_Null_For_Single_Format_Item()
    {
        var item = DataTransferItem.CreateText("hello");

        var result = item.TryGetRaw(DataFormat.Bitmap);

        Assert.Null(result);
    }

    [Fact]
    public void InProcess_Format_Works_With_DataTransferItem_Set_And_Get()
    {
        var format = DataFormat.CreateInProcessFormat<string>("my-inprocess");
        var item = new DataTransferItem();
        item.Set(format, "hello");

        var value = item.TryGetValue(format);

        Assert.Equal("hello", value);
    }

    [Fact]
    public void InProcess_Format_Coexists_With_Other_Formats_In_DataTransfer()
    {
        var inProcessFormat = DataFormat.CreateInProcessFormat<string>("my-inprocess");
        var item = new DataTransferItem();
        item.SetText("plain text");
        item.Set(inProcessFormat, "in-process data");

        var dataTransfer = new DataTransfer();
        dataTransfer.Add(item);

        Assert.Contains(DataFormat.Text, dataTransfer.Formats);
        Assert.Contains(inProcessFormat, (System.Collections.Generic.IEnumerable<DataFormat>)dataTransfer.Formats);
        Assert.Equal("plain text", item.TryGetValue(DataFormat.Text));
        Assert.Equal("in-process data", item.TryGetValue(inProcessFormat));
    }
}
