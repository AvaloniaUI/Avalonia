// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Moq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
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
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Mode = BindingMode.OneWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Priority = BindingPriority.TemplatedParent,
                Path = "Foo",
            };

            target.Object.Bind(TextBox.TextProperty, binding);

            target.Verify(x => x.Bind(
                TextBox.TextProperty, 
                It.IsAny<IObservable<object>>(), 
                BindingPriority.TemplatedParent));
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Mode = BindingMode.TwoWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Priority = BindingPriority.TemplatedParent,
                Path = "Foo",
            };

            target.Object.Bind(TextBox.TextProperty, binding);

            target.Verify(x => x.Bind(
                TextBox.TextProperty,
                It.IsAny<ISubject<object>>(),
                BindingPriority.TemplatedParent));
        }

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

        private Mock<IControl> CreateTarget(
            ITemplatedControl templatedParent = null,
            string text = null)
        {
            var result = new Mock<IControl>();

            result.Setup(x => x.GetValue(Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetValue((AvaloniaProperty)Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetValue((AvaloniaProperty)TextBox.TextProperty)).Returns(text);
            result.Setup(x => x.Bind(It.IsAny<AvaloniaProperty>(), It.IsAny<IObservable<object>>(), It.IsAny<BindingPriority>()))
                .Returns(Disposable.Empty);
            return result;
        }
    }
}
