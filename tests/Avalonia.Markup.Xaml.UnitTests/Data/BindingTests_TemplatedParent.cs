// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_TemplatedParent : XamlTestBase
    {
        [Fact]
        public void TemplateBinding_With_Null_Path_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button'>
       <Button.Template>
         <ControlTemplate>
           <TextBlock Tag='{TemplateBinding}'/>
         </ControlTemplate>
       </Button.Template>
    </Button>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();
                button.ApplyTemplate();

                var textBlock = (TextBlock)button.GetVisualChildren().Single();
                Assert.Same(button, textBlock.Tag);
            }
        }
    }
}
