// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Xaml
{
    public class BindingTests
    {
        [Fact]
        public void Can_Bind_Control_To_Non_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/perspex'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Perspex.Markup.Xaml.UnitTests.Xaml;assembly=Perspex.Markup.Xaml.UnitTests'>
    <Button Name='button' Content='Foo'>
        <Button.Tag>
            <local:NonControl Control='{Binding #button}'/>
        </Button.Tag>
    </Button>
</Window>";
                var loader = new PerspexXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                Assert.Same(button, ((NonControl)button.Tag).Control);
            }
        }
    }
}
