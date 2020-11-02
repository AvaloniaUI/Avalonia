using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Avalonia.NameGenerator.Infrastructure;
using Xunit;

namespace Avalonia.NameGenerator.Tests
{
    public class NameResolverTests
    {
        private const string NamedControl = "NamedControl.xml";
        private const string NamedControls = "NamedControls.xml";
        private const string XNamedControl = "xNamedControl.xml";
        private const string XNamedControls = "xNamedControls.xml";
        private const string NoNamedControls = "NoNamedControls.xml";
        private const string CustomControls = "CustomControls.xml";
        private const string DataTemplates = "DataTemplates.xml";
        private const string SignUpView = "SignUpView.xml";
        private const string AttachedProps = "AttachedProps.xml";

        [Theory]
        [InlineData(NamedControl)]
        [InlineData(XNamedControl)]
        [InlineData(AttachedProps)]
        public async Task Should_Resolve_Types_From_Avalonia_Markup_File_With_Named_Control(string resource)
        {
            var xaml = await LoadEmbeddedResource(resource);
            var compilation = CreateAvaloniaCompilation();
            var resolver = new NameResolver(compilation);
            var controls = resolver.ResolveNames(xaml);

            Assert.NotEmpty(controls);
            Assert.Equal(1, controls.Count);
            Assert.Equal("UserNameTextBox", controls[0].Name);
            Assert.Equal(typeof(TextBox).FullName, controls[0].TypeName);
        }

        [Theory]
        [InlineData(NamedControls)]
        [InlineData(XNamedControls)]
        public async Task Should_Resolve_Types_From_Avalonia_Markup_File_With_Named_Controls(string resource)
        {
            var xaml = await LoadEmbeddedResource(resource);
            var compilation = CreateAvaloniaCompilation();
            var resolver = new NameResolver(compilation);
            var controls = resolver.ResolveNames(xaml);

            Assert.NotEmpty(controls);
            Assert.Equal(3, controls.Count);
            Assert.Equal("UserNameTextBox", controls[0].Name);
            Assert.Equal("PasswordTextBox", controls[1].Name);
            Assert.Equal("SignUpButton", controls[2].Name);
            Assert.Equal(typeof(TextBox).FullName, controls[0].TypeName);
            Assert.Equal(typeof(TextBox).FullName, controls[1].TypeName);
            Assert.Equal(typeof(Button).FullName, controls[2].TypeName);
        }

        [Fact]
        public async Task Should_Resolve_Types_From_Avalonia_Markup_File_With_Custom_Controls()
        {
            var compilation =
                CreateAvaloniaCompilation()
                    .AddSyntaxTrees(
                        CSharpSyntaxTree.ParseText(
                            "using Avalonia.Controls;" +
                            "namespace Controls {" +
                            "  public class CustomTextBox : TextBox { }" +
                            "  public class EvilControl { }" +
                            "}"));

            var xaml = await LoadEmbeddedResource(CustomControls);
            var resolver = new NameResolver(compilation);
            var controls = resolver.ResolveNames(xaml);

            Assert.NotEmpty(controls);
            Assert.Equal(3, controls.Count);
            Assert.Equal("ClrNamespaceRoutedViewHost", controls[0].Name);
            Assert.Equal("UriRoutedViewHost", controls[1].Name);
            Assert.Equal("UserNameTextBox", controls[2].Name);
            Assert.Equal(typeof(RoutedViewHost).FullName, controls[0].TypeName);
            Assert.Equal(typeof(RoutedViewHost).FullName, controls[1].TypeName);
            Assert.Equal("Controls.CustomTextBox", controls[2].TypeName);
        }
        
        [Fact]
        public async Task Should_Not_Resolve_Named_Controls_From_Avalonia_Markup_File_Without_Named_Controls()
        {
            var xaml = await LoadEmbeddedResource(NoNamedControls);
            var compilation = CreateAvaloniaCompilation();
            var resolver = new NameResolver(compilation);
            var controls = resolver.ResolveNames(xaml);

            Assert.Empty(controls);
        }

        [Fact]
        public async Task Should_Not_Resolve_Elements_From_DataTemplates()
        {
            var xaml = await LoadEmbeddedResource(DataTemplates);
            var compilation = CreateAvaloniaCompilation();
            var resolver = new NameResolver(compilation);
            var controls = resolver.ResolveNames(xaml);
            
            Assert.NotEmpty(controls);
            Assert.Equal(2, controls.Count);
            Assert.Equal("UserNameTextBox", controls[0].Name);
            Assert.Equal("NamedListBox", controls[1].Name);
            Assert.Equal(typeof(TextBox).FullName, controls[0].TypeName);
            Assert.Equal(typeof(ListBox).FullName, controls[1].TypeName);
        }

        [Fact]
        public async Task Should_Resolve_Names_From_Complex_Views()
        {
            var compilation =
                CreateAvaloniaCompilation()
                    .AddSyntaxTrees(
                        CSharpSyntaxTree.ParseText(
                            "using Avalonia.Controls;" +
                            "namespace Controls {" +
                            "  public class CustomTextBox : TextBox { }" +
                            "}"));
            
            var xaml = await LoadEmbeddedResource(SignUpView);
            var resolver = new NameResolver(compilation);
            var controls = resolver.ResolveNames(xaml);
            
            Assert.NotEmpty(controls);
            Assert.Equal(9, controls.Count);
            Assert.Equal("UserNameTextBox", controls[0].Name);
            Assert.Equal("UserNameValidation", controls[1].Name);
            Assert.Equal("PasswordTextBox", controls[2].Name);
            Assert.Equal("PasswordValidation", controls[3].Name);
            Assert.Equal("AwesomeListView", controls[4].Name);
            Assert.Equal("ConfirmPasswordTextBox", controls[5].Name);
            Assert.Equal("ConfirmPasswordValidation", controls[6].Name);
            Assert.Equal("SignUpButton", controls[7].Name);
            Assert.Equal("CompoundValidation", controls[8].Name);
        }
        
        private static CSharpCompilation CreateAvaloniaCompilation(string name = "AvaloniaCompilation2")
        {
            var compilation = CSharpCompilation
                .Create(name, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(ITypeDescriptorContext).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(ISupportInitialize).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(TypeConverterAttribute).Assembly.Location));
            
            var avaloniaAssemblyLocation = typeof(TextBlock).Assembly.Location;
            var avaloniaAssemblyDirectory = Path.GetDirectoryName(avaloniaAssemblyLocation);
            var avaloniaAssemblyReferences = Directory
                .EnumerateFiles(avaloniaAssemblyDirectory!)
                .Where(file => file.EndsWith(".dll") && file.Contains("Avalonia"))
                .Select(file => MetadataReference.CreateFromFile(file))
                .ToList();

            return compilation.AddReferences(avaloniaAssemblyReferences);
        }

        private static async Task<string> LoadEmbeddedResource(string shortResourceName)
        {
            var assembly = typeof(NameResolverTests).Assembly;
            var fullResourceName = assembly
                .GetManifestResourceNames()
                .First(name => name.EndsWith(shortResourceName));
            
            await using var stream = assembly.GetManifestResourceStream(fullResourceName);
            using var reader = new StreamReader(stream!);
            return await reader.ReadToEndAsync();
        }
    }
}