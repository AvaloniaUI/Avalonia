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

        [Fact]
        public void Can_Set_DataContext_In_DataTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/perspex'
        xmlns:local='clr-namespace:Perspex.Markup.Xaml.UnitTests;assembly=Perspex.Markup.Xaml.UnitTests'>
    <Window.DataTemplates>
        <DataTemplate DataType='{Type local:TestViewModel}'>
            <Canvas Name='foo' DataContext='{Binding Child}'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{Binding Child}'/>
</Window>";
                var loader = new PerspexXamlLoader();
                var window = (Window)loader.Load(xaml);
                var target = window.FindControl<ContentControl>("target");

                var viewModel = new TestViewModel
                {
                    String = "Root",
                    Child = new TestViewModel
                    {
                        String = "Child",
                        Child = new TestViewModel
                        {
                            String = "Grandchild",
                        }
                    },
                };

                window.DataContext = viewModel;

                window.ApplyTemplate();
                target.ApplyTemplate();
                ((ContentPresenter)target.Presenter).UpdateChild();

                var canvas = (Canvas)target.Presenter.Child;
                Assert.Same(viewModel, target.DataContext);
                Assert.Same(viewModel.Child.Child, canvas.DataContext);
            }
        }

        [Fact]
        public void DataTemplate_Without_Data_Type_Should_Not_Cause_StackOverflow()
        {
            var xaml = @"<Window xmlns='https://github.com/perspex'>
                <ContentControl Content='{Binding Integer}'>
                    <ContentControl.DataTemplates>
                        <DataTemplate>
                            <Button>1</Button>
                        </DataTemplate>
                    </ContentControl.DataTemplates>
                </ContentControl>
            </Window>";
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var perspexLoader = new PerspexXamlLoader();
                var window = (Window)perspexLoader.Load(xaml);
                window.DataContext = new TestViewModel();
                TestServices.StyledWindow.LayoutManager.ExecuteInitialLayoutPass(window);
            }
        }

        [Fact]
        public void DataTemplate_With_Child_That_Resolves_DataTemplate_Should_Not_Cause_StackOverflow()
        {
            var xaml = @"<Window xmlns='https://github.com/perspex'
                            xmlns:local='clr-namespace:Perspex.Markup.Xaml.UnitTests;assembly=Perspex.Markup.Xaml.UnitTests'>
                <ContentControl Content='{Binding Child}'>
                    <ContentControl.DataTemplates>
                        <DataTemplate DataType='{Type local:TestViewModel}'>
                            <Button Content='{Binding Child}' />
                        </DataTemplate>
                    </ContentControl.DataTemplates>
                </ContentControl>
            </Window>";
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var perspexLoader = new PerspexXamlLoader();
                var window = (Window)perspexLoader.Load(xaml);
                var model = new TestViewModel();
                model.Child = model;
                window.DataContext = model;
                TestServices.StyledWindow.LayoutManager.ExecuteInitialLayoutPass(window);
            }
        }
    }
}
