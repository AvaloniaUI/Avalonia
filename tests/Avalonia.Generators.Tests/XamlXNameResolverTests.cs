using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Avalonia.Generators.Tests.Views;
using Xunit;

namespace Avalonia.Generators.Tests;

public class XamlXNameResolverTests
{
    [Theory]
    [InlineData(View.NamedControl)]
    [InlineData(View.XNamedControl)]
    [InlineData(View.AttachedProps)]
    public async Task Should_Resolve_Types_From_Avalonia_Markup_File_With_Named_Control(string resource)
    {
        var xaml = await View.Load(resource);
        var controls = ResolveNames(xaml);

        Assert.NotEmpty(controls);
        var control = Assert.Single(controls);
        Assert.Equal("UserNameTextBox", control.Name);
        Assert.Contains(typeof(TextBox).FullName!, control.TypeName);
    }

    [Theory]
    [InlineData(View.NamedControls)]
    [InlineData(View.XNamedControls)]
    public async Task Should_Resolve_Types_From_Avalonia_Markup_File_With_Named_Controls(string resource)
    {
        var xaml = await View.Load(resource);
        var controls = ResolveNames(xaml);

        Assert.NotEmpty(controls);
        Assert.Equal(3, controls.Count);
        Assert.Equal("UserNameTextBox", controls[0].Name);
        Assert.Equal("PasswordTextBox", controls[1].Name);
        Assert.Equal("SignUpButton", controls[2].Name);
        Assert.Contains(typeof(TextBox).FullName!, controls[0].TypeName);
        Assert.Contains(typeof(TextBox).FullName!, controls[1].TypeName);
        Assert.Contains(typeof(Button).FullName!, controls[2].TypeName);
    }

    [Fact]
    public async Task Should_Resolve_Types_From_Avalonia_Markup_File_With_Custom_Controls()
    {
        var xaml = await View.Load(View.CustomControls);
        var controls = ResolveNames(xaml);

        Assert.NotEmpty(controls);
        Assert.Equal(3, controls.Count);
        Assert.Equal("ClrNamespaceColorPicker", controls[0].Name);
        Assert.Equal("UriColorPicker", controls[1].Name);
        Assert.Equal("UserNameTextBox", controls[2].Name);
        Assert.Contains(typeof(ColorPicker).FullName!, controls[0].TypeName);
        Assert.Contains(typeof(ColorPicker).FullName!, controls[1].TypeName);
        Assert.Contains("Controls.CustomTextBox", controls[2].TypeName);
    }

    [Fact]
    public async Task Should_Resolve_Types_From_Avalonia_Markup_File_When_Types_Contains_Generic_Arguments()
    {
        var xaml = await View.Load(View.ViewWithGenericBaseView);
        var controls = ResolveNames(xaml);
        Assert.Equal(2, controls.Count);
        
        var currentControl = controls[0];
        Assert.Equal("Root", currentControl.Name);
        Assert.Equal("global::Sample.App.BaseView<global::System.String>", currentControl.TypeName);

        currentControl = controls[1];
        Assert.Equal("NotAsRootNode", currentControl.Name);
        Assert.Contains("Sample.App.BaseView", currentControl.TypeName);
        Assert.Equal("global::Sample.App.BaseView<global::System.Int32>", currentControl.TypeName);
    }

    [Fact]
    public async Task Should_Not_Resolve_Named_Controls_From_Avalonia_Markup_File_Without_Named_Controls()
    {
        var xaml = await View.Load(View.NoNamedControls);
        var controls = ResolveNames(xaml);

        Assert.Empty(controls);
    }

    [Fact]
    public async Task Should_Not_Resolve_Elements_From_DataTemplates()
    {
        var xaml = await View.Load(View.DataTemplates);
        var controls = ResolveNames(xaml);

        Assert.NotEmpty(controls);
        Assert.Equal(2, controls.Count);
        Assert.Equal("UserNameTextBox", controls[0].Name);
        Assert.Equal("NamedListBox", controls[1].Name);
        Assert.Contains(typeof(TextBox).FullName!, controls[0].TypeName);
        Assert.Contains(typeof(ListBox).FullName!, controls[1].TypeName);
    }

    [Fact]
    public async Task Should_Resolve_Names_From_Complex_Views()
    {
        var xaml = await View.Load(View.SignUpView);
        var controls = ResolveNames(xaml);

        Assert.NotEmpty(controls);
        Assert.Equal(10, controls.Count);
        Assert.Equal("UserNameTextBox", controls[0].Name);
        Assert.Equal("UserNameValidation", controls[1].Name);
        Assert.Equal("PasswordTextBox", controls[2].Name);
        Assert.Equal("PasswordValidation", controls[3].Name);
        Assert.Equal("AwesomeListView", controls[4].Name);
        Assert.Equal("ConfirmPasswordTextBox", controls[5].Name);
        Assert.Equal("ConfirmPasswordValidation", controls[6].Name);
        Assert.Equal("SignUpButtonDescription", controls[7].Name);
        Assert.Equal("SignUpButton", controls[8].Name);
        Assert.Equal("CompoundValidation", controls[9].Name);
    }

    private static IReadOnlyList<ResolvedName> ResolveNames(string xaml)
    {
        var nameResolver = new XamlXNameResolver();

        // Step 1: parse XAML as xml nodes, without any type information.
        var classResolver = new XamlXViewResolver(MiniCompiler.CreateNoop());
        var classInfo = classResolver.ResolveView(xaml, CancellationToken.None);
        Assert.NotNull(classInfo);
        var names = nameResolver.ResolveXmlNames(classInfo.Xaml, CancellationToken.None);

        // Step 2: use compilation context to resolve types
        var compilation =
            View.CreateAvaloniaCompilation()
                .WithCustomTextBox()
                .WithBaseView();
        return names.ResolveNames(compilation, nameResolver).ToArray();
    }
}
