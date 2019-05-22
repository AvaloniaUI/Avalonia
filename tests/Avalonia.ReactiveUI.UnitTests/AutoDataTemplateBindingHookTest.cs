// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;
using ReactiveUI;
using Avalonia.ReactiveUI;
using Avalonia.UnitTests;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Splat;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class AutoDataTemplateBindingHookTest
    {
        public class NestedViewModel : ReactiveObject { }

        public class NestedView : ReactiveUserControl<NestedViewModel> { }

        public class ExampleViewModel : ReactiveObject 
        {
            public ObservableCollection<NestedViewModel> Items { get; } = new ObservableCollection<NestedViewModel>();
        }

        public class ExampleView : ReactiveUserControl<ExampleViewModel>
        {
            public ListBox List { get; } = new ListBox();

            public ExampleView()
            {
                Content = List;
                ViewModel = new ExampleViewModel();
                this.OneWayBind(ViewModel, x => x.Items, x => x.List.Items);
            }
        }

        public AutoDataTemplateBindingHookTest() 
        {
            Locator.CurrentMutable.RegisterConstant(new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));
            Locator.CurrentMutable.Register(() => new ExampleView(), typeof(IViewFor<ExampleViewModel>));
        }

        [Fact]
        public void Should_Apply_Data_Template_Binding_When_No_Template_Is_Set()
        {
            var view = new ExampleView();
            var root = new TestRoot 
            { 
                Child = view
            };
            Assert.NotNull(view.List.ItemTemplate);
        }
    }
}