// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Xaml
{
    public class DataTemplateTests
    {
        [Fact]
        public void DataTemplate_Can_Contain_Name()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/perspex'
        xmlns:sys='clr-namespace:System;assembly=mscorlib'>
    <Window.DataTemplates>
        <DataTemplate DataType='{Type sys:String}'>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
                var loader = new PerspexXamlLoader();
                var window = (Window)loader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");

                window.ApplyTemplate();
                target.ApplyTemplate();
                ((ContentPresenter)target.Presenter).UpdateChild();

                Assert.IsType<Canvas>(target.Presenter.Child);
            }
        }
    }
}
