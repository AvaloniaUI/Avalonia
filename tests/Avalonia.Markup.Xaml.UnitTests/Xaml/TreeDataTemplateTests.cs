// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class TreeDataTemplateTests : XamlTestBase
    {
        [Fact]
        public void Binding_Should_Be_Assigned_To_ItemsSource_Instead_Of_Bound()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = "<DataTemplates xmlns='https://github.com/avaloniaui'><TreeDataTemplate ItemsSource='{Binding}'/></DataTemplates>";
                var loader = new AvaloniaXamlLoader();
                var templates = (DataTemplates)loader.Load(xaml);
                var template = (TreeDataTemplate)(templates.First());

                Assert.IsType<Binding>(template.ItemsSource);
            }                
        }
    }
}
