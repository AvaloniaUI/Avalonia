// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Moq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Xunit;
using System.Reactive.Disposables;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_TemplatedParent
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
           <TextBlock Text='{TemplateBinding}'/>
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
                Assert.Equal("Avalonia.Controls.Button", textBlock.Text);
            }
        }
    }
}
