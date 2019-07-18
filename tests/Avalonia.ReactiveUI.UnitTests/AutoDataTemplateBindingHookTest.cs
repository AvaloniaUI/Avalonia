// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;
using ReactiveUI;
using Avalonia.ReactiveUI;
using Avalonia.UnitTests;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.VisualTree;
using Avalonia.Controls.Presenters;
using Splat;
using System.Threading.Tasks;
using System;

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
            public ItemsControl List { get; } = new ItemsControl();

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
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.Register(() => new NestedView(), typeof(IViewFor<NestedViewModel>));
        }

        [Fact]
        public void Should_Apply_Data_Template_Binding_When_No_Template_Is_Set()
        {
            var view = new ExampleView();
            Assert.NotNull(view.List.ItemTemplate);
        }

        [Fact]
        public void Should_Use_View_Model_View_Host_As_Data_Template()
        {
            var view = new ExampleView();
            view.ViewModel.Items.Add(new NestedViewModel());

            view.List.Template = GetTemplate();
            view.List.ApplyTemplate();
            view.List.Presenter.ApplyTemplate();

            var child = view.List.Presenter.Panel.Children[0];
            var container = (ContentPresenter) child;
            container.UpdateChild();

            Assert.IsType<ViewModelViewHost>(container.Child);
        }

        [Fact]
        public void Should_Resolve_And_Embedd_Appropriate_View_Model()
        {
            var view = new ExampleView();
            var root = new TestRoot { Child = view };
            view.ViewModel.Items.Add(new NestedViewModel());

            view.List.Template = GetTemplate();
            view.List.ApplyTemplate();
            view.List.Presenter.ApplyTemplate();

            var child = view.List.Presenter.Panel.Children[0];
            var container = (ContentPresenter) child;
            container.UpdateChild();

            var host = (ViewModelViewHost) container.Child;
            Assert.IsType<NestedViewModel>(host.ViewModel);
            Assert.IsType<NestedViewModel>(host.DataContext);

            host.DataContext = "changed context";
            Assert.IsType<string>(host.ViewModel);
            Assert.IsType<string>(host.DataContext);
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ItemsControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
                    }.RegisterInNameScope(scope)
                };
            });
        }
    }
}
