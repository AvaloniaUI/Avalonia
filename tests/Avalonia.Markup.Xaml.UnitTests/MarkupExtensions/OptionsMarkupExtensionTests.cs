using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Metadata;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;

public class OptionsMarkupExtensionTests : XamlTestBase
{
    public static Func<object, bool> RaisedOption;
    public static int? ObjectsCreated;

    [Fact]
    public void Resolve_Default_Value()
    {
        using var _ = SetupTestGlobals("default");

        var xaml = @"
<TextBlock xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtension Default=""Hello World""}' />";

        var textBlock = (TextBlock)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal("Hello World", textBlock.Text);
    }

    [Fact]
    public void Resolve_Default_Value_From_Ctor()
    {
        using var _ = SetupTestGlobals("default");

        var xaml = @"
<TextBlock xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtension ""Hello World"", OptionB=""Im Android""}' />";

        var textBlock = (TextBlock)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal("Hello World", textBlock.Text);
    }

    [Fact]
    public void Resolve_Implicit_Default_Value_Ref_Type()
    {
        using var _ = SetupTestGlobals("default");

        var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             Tag='{local:OptionsMarkupExtension OptionA=""Hello World"", x:DataType=x:String}' />";

        var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(null, userControl.Tag);
    }

    [Fact]
    public void Resolve_Implicit_Default_Value_Val_Type()
    {
        using var _ = SetupTestGlobals("default");

        var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             Height='{local:OptionsMarkupExtension OptionA=10}' />";

        var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(0d, userControl.Height);
    }

    [Fact]
    public void Resolve_Implicit_Default_Value_Avalonia_Val_Type()
    {
        using var _ = SetupTestGlobals("default");

        var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             Margin='{local:OptionsMarkupExtension OptionA=10}' />";

        var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(new Thickness(0), userControl.Margin);
    }

    [Theory]
    [InlineData("option 1", "Im Option 1")]
    [InlineData("option 2", "Im Option 2")]
    [InlineData("3", "Im Option 3")]
    [InlineData("unknown", "Default value")]
    public void Resolve_Expected_Value_Per_Option(object option, string expectedResult)
    {
        using var _ = SetupTestGlobals(option);

        var xaml = @"
<TextBlock xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtension ""Default value"",
                OptionA=""Im Option 1"", OptionB=""Im Option 2"",
                OptionNumber=""Im Option 3""}' />";

        var textBlock = (TextBlock)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(expectedResult, textBlock.Text);
    }

    [Theory]
    [InlineData("option 1", "Im Option 1")]
    [InlineData("option 2", "Im Option 2")]
    [InlineData("3", "Im Option 3")]
    [InlineData("unknown", "Default value")]
    public void Resolve_Expected_Value_Per_Option_Create_Single_Object(object option, string expectedResult)
    {
        using var _ = SetupTestGlobals(option);

        var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Content>
        <local:OptionsMarkupExtension>
            <local:OptionsMarkupExtension.Default>
                <local:ChildObject Name=""Default value"" />
            </local:OptionsMarkupExtension.Default>
            <local:OptionsMarkupExtension.OptionA>
                <local:ChildObject Name=""Im Option 1"" />
            </local:OptionsMarkupExtension.OptionA>
            <local:OptionsMarkupExtension.OptionB>
                <local:ChildObject Name=""Im Option 2"" />
            </local:OptionsMarkupExtension.OptionB>
            <local:OptionsMarkupExtension.OptionNumber>
                <local:ChildObject Name=""Im Option 3"" />
            </local:OptionsMarkupExtension.OptionNumber>
        </local:OptionsMarkupExtension>
    </ContentControl.Content>
</ContentControl>";

        var contentControl = (ContentControl)AvaloniaRuntimeXamlLoader.Load(xaml);
        var obj = Assert.IsType<ChildObject>(contentControl.Content);

        Assert.Equal(expectedResult, obj.Name);
        Assert.Equal(1, ObjectsCreated);
    }

    [Fact]
    public void Convert_Bcl_Type()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<Border xmlns='https://github.com/avaloniaui'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Height='{local:OptionsMarkupExtension OptionA=50.1}' />";

        var border = (Border)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(50.1, border.Height);
    }

    [Fact]
    public void Convert_Avalonia_Type()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<Border xmlns='https://github.com/avaloniaui'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Padding='{local:OptionsMarkupExtension OptionA=""10, 8, 10, 8""}' />";

        var border = (Border)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(new Thickness(10, 8, 10, 8), border.Padding);
    }

    [PlatformFact(TestPlatforms.Windows | TestPlatforms.Linux, "TypeArguments test is failing on macOS from SRE emit")]
    public void Respect_Custom_TypeArgument()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<TextBlock xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Tag='{local:OptionsMarkupExtensionWithGeneric Default=20, OptionA=""10, 10, 10, 10"", x:TypeArguments=Thickness}' />";

        var textBlock = (TextBlock)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(new Thickness(10, 10, 10, 10), textBlock.Tag);
    }

    [Fact]
    public void Allow_Nester_Markup_Extensions()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>
    <Border Background='{local:OptionsMarkupExtension OptionA={StaticResource brush}}'/>
</UserControl>";

        var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
        var border = (Border)userControl.Content!;

        Assert.Equal(Color.Parse("#ff506070"), ((ISolidColorBrush)border.Background!).Color);
    }

    [Fact]
    public void Allow_Nester_On_Platform_Markup_Extensions()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<Border xmlns='https://github.com/avaloniaui'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Margin='{local:OptionsMarkupExtension OptionA={local:OptionsMarkupExtensionMinimal OptionA=""10,10,10,10""}}' />";

        var border = (Border)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(new Thickness(10), border.Margin);
    }

    [Fact]
    public void Support_Xml_Syntax()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<Border xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border.Background>
        <local:OptionsMarkupExtension>
            <local:OptionsMarkupExtension.OptionA>
                <SolidColorBrush Color='#ff506070' />
            </local:OptionsMarkupExtension.OptionA>
        </local:OptionsMarkupExtension>
    </Border.Background>
</Border>";

        var border = (Border)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(Color.Parse("#ff506070"), ((ISolidColorBrush)border.Background!).Color);
    }

    [PlatformFact(TestPlatforms.Windows | TestPlatforms.Linux, "TypeArguments test is failing on macOS from SRE emit")]
    public void Support_Xml_Syntax_With_Custom_TypeArguments()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<Border xmlns='https://github.com/avaloniaui'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border.Tag>
        <local:OptionsMarkupExtensionWithGeneric x:TypeArguments='Thickness' OptionA='10, 10, 10, 10' Default='20' />
    </Border.Tag>
</Border>";

        var border = (Border)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(new Thickness(10, 10, 10, 10), border.Tag);
    }

    [Theory]
    [InlineData("option 1", "#ff506070")]
    [InlineData("3", "#000")]
    public void Support_Special_On_Syntax(object option, string color)
    {
        using var _ = SetupTestGlobals(option);

        var xaml = @"
<Border xmlns='https://github.com/avaloniaui'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border.Background>
        <local:OptionsMarkupExtension>
            <On Options='OptionA, OptionB'>
                <SolidColorBrush Color='#ff506070' />
            </On>
            <On Options=' OptionNumber '>
                <SolidColorBrush Color='#000' />
            </On>
        </local:OptionsMarkupExtension>
    </Border.Background>
</Border>";

        var border = (Border)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal(Color.Parse(color), ((ISolidColorBrush)border.Background!).Color);
    }

    [Fact]
    public void Support_Control_Inside_Xml_Syntax()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <local:OptionsMarkupExtension>
        <local:OptionsMarkupExtension.OptionA>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.OptionA>
    </local:OptionsMarkupExtension>
</UserControl>";

        var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
        var button = (Button)userControl.Content!;

        Assert.Equal("Hello World", button.Content);
    }

    [Fact]
    public void Support_Default_Control_Inside_Xml_Syntax()
    {
        using var _ = SetupTestGlobals("unknown");

        var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <local:OptionsMarkupExtension>
        <local:OptionsMarkupExtension.Default>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.Default>
    </local:OptionsMarkupExtension>
</UserControl>";

        var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
        var button = (Button)userControl.Content!;

        Assert.Equal("Hello World", button.Content);
    }

    [Fact]
    public void Support_Complex_Property_Setters_Dictionary()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Color x:Key='Color1'>Black</Color>
    <local:OptionsMarkupExtension x:Key='MyKey'>
        <local:OptionsMarkupExtension.OptionA>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.OptionA>
    </local:OptionsMarkupExtension>
    <Color x:Key='Color2'>White</Color>
</ResourceDictionary>";

        var resourceDictionary = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);
        var button = Assert.IsType<Button>(resourceDictionary["MyKey"]);
        Assert.Equal("Hello World", button.Content);
        Assert.Equal(Colors.Black, resourceDictionary["Color1"]);
        Assert.Equal(Colors.White, resourceDictionary["Color2"]);
    }

    [Fact]
    public void Support_Complex_Property_Setters_List()
    {
        using var _ = SetupTestGlobals("option 1");

        var xaml = @"
<Panel xmlns='https://github.com/avaloniaui'
       xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock />
    <local:OptionsMarkupExtension>
        <local:OptionsMarkupExtension.OptionA>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.OptionA>
    </local:OptionsMarkupExtension>
    <TextBox />
</Panel>";

        var panel = (Panel)AvaloniaRuntimeXamlLoader.Load(xaml);
        Assert.Equal(3, panel.Children.Count);
        Assert.IsType<Button>(panel.Children[1]);
    }

    [Theory]
    [InlineData("option 1", "foo")]
    [InlineData("option 2", "bar")]
    public void BindingExtension_Works_Inside_Of_OptionsMarkupExtension(string option, string expected)
    {
        using var _ = SetupTestGlobals(option);

        var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <x:String x:Key='text'>foo</x:String>
    </UserControl.Resources>

    <TextBlock Name='textBlock' Text='{local:OptionsMarkupExtension OptionA={CompiledBinding Source={StaticResource text}}, OptionB=bar}'/>
</UserControl>";

        var window = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
        var textBlock = window.FindControl<TextBlock>("textBlock");

        Assert.Equal(expected, textBlock.Text);
    }

    [Fact]
    public void Resolve_Expected_Value_With_Method_Without_ServiceProvider()
    {
        using var _ = SetupTestGlobals(2);

        var xaml = @"
<TextBlock xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtensionNoServiceProvider OptionB=""Im Option 2"", OptionA=""Im Option 1""}' />";

        var textBlock = (TextBlock)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.Equal("Im Option 2", textBlock.Text);
    }

    [Fact]
    public void Resolve_Expected_Value_Minimal_Extension()
    {
        var xaml = @"
<TextBlock xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           IsVisible='{local:OptionsMarkupExtensionMinimal OptionA=True}' />";

        var textBlock = (TextBlock)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.True(textBlock.IsSet(Visual.IsVisibleProperty));
        Assert.True(textBlock.IsVisible);
    }

    [Fact]
    public void Resolve_Expected_Value_Extension_With_Property()
    {
        var xaml = @"
<TextBlock xmlns='https://github.com/avaloniaui'
           xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           IsVisible='{local:OptionsMarkupExtensionWithProperty OptionA=True, Property=5}' />";

        var textBlock = (TextBlock)AvaloniaRuntimeXamlLoader.Load(xaml);

        Assert.True(textBlock.IsSet(Visual.IsVisibleProperty));
        Assert.True(textBlock.IsVisible);
    }

    private static IDisposable SetupTestGlobals(object acceptedOption)
    {
        RaisedOption = o => o.Equals(acceptedOption);
        ObjectsCreated = 0;
        return Disposable.Create(() =>
        {
            RaisedOption = null;
            ObjectsCreated = null;
        });
    }
}

public class OptionsMarkupExtension : OptionsMarkupExtensionBase<object, On>
{
    public OptionsMarkupExtension()
    {

    }

    public OptionsMarkupExtension(object defaultValue)
    {
        Default = defaultValue;
    }
}

public class OptionsMarkupExtension<TReturn> : OptionsMarkupExtensionBase<TReturn, On<TReturn>>
{
    public OptionsMarkupExtension()
    {

    }

    public OptionsMarkupExtension(TReturn defaultValue)
    {
        Default = defaultValue;
    }
}

public class OptionsMarkupExtensionBase<TReturn, TOn> : IAddChild<TOn>
    where TOn : On<TReturn>
{
    [MarkupExtensionOption("option 1")]
    public TReturn OptionA { get; set; }

    [MarkupExtensionOption("option 2")]
    public TReturn OptionB { get; set; }

    [MarkupExtensionOption(3)]
    public TReturn OptionNumber { get; set; }

    [Content]
    [MarkupExtensionDefaultOption]
    public TReturn Default { get; set; }

    public bool ShouldProvideOption(IServiceProvider serviceProvider, string option)
    {
        return OptionsMarkupExtensionTests.RaisedOption(option);
    }

    public TReturn ProvideValue(IServiceProvider serviceProvider) { throw null; }

    public void AddChild(TOn child) {}
}

public class OptionsMarkupExtensionNoServiceProvider
{
    [MarkupExtensionOption(1)]
    public object OptionA { get; set; }

    [MarkupExtensionOption(2)]
    public object OptionB { get; set; }

    [Content]
    [MarkupExtensionDefaultOption]
    public object Default { get; set; }

    public static bool ShouldProvideOption(int option)
    {
        return OptionsMarkupExtensionTests.RaisedOption(option);
    }

    public object ProvideValue(IServiceProvider serviceProvider) { throw null; }
}

public class OptionsMarkupExtensionMinimal
{
    [MarkupExtensionOption(11.0)]
    public bool OptionA { get; set; }

    public static bool ShouldProvideOption(double option) => option > 0;

    public object ProvideValue() { throw null; }
}

public class OptionsMarkupExtensionWithProperty
{
    [MarkupExtensionOption(5)]
    public bool OptionA { get; set; }

    public int Property { get; set; }

    public bool ShouldProvideOption(int option) => option == Property;

    public object ProvideValue() { throw null; }
}

public class OptionsMarkupExtensionWithGeneric<TResult>
{
    [MarkupExtensionOption("option 1")]
    public TResult OptionA { get; set; }

    [Content]
    [MarkupExtensionDefaultOption]
    public TResult Default { get; set; }

    public bool ShouldProvideOption(string option)
    {
        return OptionsMarkupExtensionTests.RaisedOption(option);
    }

    public TResult ProvideValue() { throw null; }
}

public class ChildObject
{
    public string Name { get; set; }

    public ChildObject()
    {
        OptionsMarkupExtensionTests.ObjectsCreated++;
    }
}
