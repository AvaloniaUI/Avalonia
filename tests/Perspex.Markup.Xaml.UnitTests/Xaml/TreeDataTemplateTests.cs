// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls.Templates;
using Perspex.Markup.Xaml.Data;
using Perspex.Markup.Xaml.Templates;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Xaml
{
    public class TreeDataTemplateTests
    {
        [Fact]
        public void Binding_Should_Be_Assigned_To_ItemsSource_Instead_Of_Bound()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = "<DataTemplates xmlns='https://github.com/perspex'><TreeDataTemplate ItemsSource='{Binding}'/></DataTemplates>";
                var loader = new PerspexXamlLoader();
                var templates = (DataTemplates)loader.Load(xaml);
                var template = (TreeDataTemplate)(templates.First());

                Assert.IsType<Binding>(template.ItemsSource);
            }                
        }
    }
}
