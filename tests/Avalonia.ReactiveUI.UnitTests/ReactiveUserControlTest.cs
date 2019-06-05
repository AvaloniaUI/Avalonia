// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.UnitTests;
using ReactiveUI;
using Splat;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class ReactiveUserControlTest
    {
        public class ExampleViewModel : ReactiveObject { }

        public class ExampleView : ReactiveUserControl<ExampleViewModel> { }

        [Fact]
        public void Data_Context_Should_Stay_In_Sync_With_Reactive_User_Control_View_Model() 
        {
            var view = new ExampleView();
            var viewModel = new ExampleViewModel();
            Assert.Null(view.ViewModel);

            view.DataContext = viewModel;
            Assert.Equal(view.ViewModel, viewModel);
            Assert.Equal(view.DataContext, viewModel);

            view.DataContext = null;
            Assert.Null(view.ViewModel);
            Assert.Null(view.DataContext);
        }
    }
}