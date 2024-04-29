#nullable enable

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class ResourceIncludeTests : XamlTestBase
    {
        [Fact]
        public void ResourceInclude_Loads_ResourceDictionary()
        {
            var documents = new[]
            {
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resource.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
</ResourceDictionary>"),
                new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source='avares://Tests/Resource.xaml'/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</UserControl.Resources>

<Border Name='border' Background='{StaticResource brush}'/>
</UserControl>")
            };

            using (StartWithResources())
            {
                var compiled = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                var userControl = Assert.IsType<UserControl>(compiled[1]);
                var border = userControl.GetControl<Border>("border");

                var brush = (ISolidColorBrush)border.Background!;
                Assert.Equal(0xff506070, brush.Color.ToUInt32());
            }
        }

        [Fact]
        public void Missing_ResourceKey_In_ResourceInclude_Does_Not_Cause_StackOverflow()
        {
            var app = Application.Current;
            var documents = new[]
            {
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resource.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<StaticResource x:Key='brush' ResourceKey='missing' />
</ResourceDictionary>"),
                new RuntimeXamlLoaderDocument(app, @"
<Application xmlns='https://github.com/avaloniaui'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source='avares://Tests/Resource.xaml'/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
</Application>")
            };

            using (StartWithResources())
            {
                try
                {
                    AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                }
                catch (KeyNotFoundException)
                {

                }
            }
        }
        
        [Fact]
        public void ResourceInclude_Should_Be_Allowed_To_Have_Key_In_Custom_Container()
        {
            var app = Application.Current;
            var documents = new[]
            {
                new RuntimeXamlLoaderDocument(new Uri("avares://Demo/en-us.axaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <x:String x:Key='OkButton'>OK</x:String>
</ResourceDictionary>"),
                new RuntimeXamlLoaderDocument(app, @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <ResourceDictionary.MergedDictionaries>
        <local:LocaleCollection>
            <ResourceInclude Source='avares://Demo/en-us.axaml' x:Key='English' />
        </local:LocaleCollection>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
            };

            using (StartWithResources())
            {
                var groups = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                var res = Assert.IsType<ResourceDictionary>(groups[1]);

                Assert.True(res.TryGetResource("OkButton", null, out var val));
                Assert.Equal("OK", val);
            }
        }

        private IDisposable StartWithResources(params (string, string)[] assets)
        {
            var assetLoader = new MockAssetLoader(assets);
            var services = new TestServices(assetLoader: assetLoader);
            return UnitTestApplication.Start(services);
        }
    }
    
    // See https://github.com/AvaloniaUI/Avalonia/issues/11172
    public class LocaleCollection : IResourceProvider
    {
        private readonly Dictionary<object, IResourceProvider> _langs = new();

        public IResourceHost? Owner { get; private set; }

        public bool HasResources => true;

        public event EventHandler? OwnerChanged
        {
            add { }
            remove { }
        }

        public void AddOwner(IResourceHost owner) => Owner = owner;

        public void RemoveOwner(IResourceHost owner) => Owner = null;

        public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
        {
            if (_langs.TryGetValue("English", out var res))
            {
                return res.TryGetResource(key, theme, out value);
            }
            value = null;
            return false;
        }

        // Allow Avalonia to use this class as a collection, requires x:Key on the IResourceProvider 
        public void Add(object k, IResourceProvider v) => _langs.Add(k, v);
    }
}
