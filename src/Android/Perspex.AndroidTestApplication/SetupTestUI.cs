using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ReactiveUI;
using Perspex.Android.Platform.CanvasPlatform;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Android;
using Perspex.Styling;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Controls;
using Perspex.Markup.Xaml.Data;
using Perspex.Controls.Presenters;

namespace Perspex.AndroidTestApplication
{
    public class SetupTestUI
    {
        private static Style HeaderedContentControlStyle()
        {
            return new Style(x => x.OfType<HeaderedContentControl>())
            {
                Setters = new[]
                                {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<HeaderedContentControl>(control=> {
                           return new Border()
                        {
                            [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                            Child = new StackPanel()
                            {
                                Children = new Controls.Controls()
                                {
                                    new ContentPresenter
                                    {
                                        Name = "headercontentPresenter",
                                        [~ContentPresenter.ContentProperty] = control[~HeaderedContentControl.HeaderProperty],
                                    },
                                    new ContentPresenter
                                    {
                                        Name = "contentPresenter",
                                        [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                                    }
                                }
                            }
                        };
                        })),
                    },
            };
        }



        private Window BuildPlatforSetup()
        {
            Perspex.Application.Current.Styles.Add(HeaderedContentControlStyle());
            var vm = new PlatformSetupViewModel();
            vm.PointUnit = AndroidPlatform.Instance.DefaultPointUnit.ToString();
            vm.ViewDrawType = AndroidPlatform.Instance.DefaultViewDrawType.ToString();
            var lbPointUnit = new ListBox()
            {
                Name = "PointUnit",
                Items = vm.PointUnits,
                [ListBox.SelectedItemProperty] = vm.PointUnit
            };
            var lbViewDrawType = new ListBox()
            {
                Name = "ViewDrawType",
                Items = vm.ViewDrawTypes,
                [ListBox.SelectedItemProperty] = vm.ViewDrawType
            };
            var window = new Window()
            {
                DataContext = vm,
                Content = new Grid()
                {
                    Margin = new Thickness(20, 100, 0, 0),
                    RowDefinitions = new RowDefinitions() {
                        new RowDefinition() { Height = GridLength.Auto },
                        new RowDefinition()
                    },
                    Children = new Controls.Controls()
                    {
                        new Button()
                        {
                            [Grid.RowProperty] = 1,
                            HorizontalAlignment = Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Layout.VerticalAlignment.Center,
                            Content = "Start application",
                            Command = vm.Command
                        },
                        new StackPanel()
                        {
                            [Grid.RowProperty] = 0,
                            Orientation = Orientation.Horizontal,
                            Gap=10,
                            Children= new Controls.Controls
                            {
                                new HeaderedContentControl() {Content = lbPointUnit, Header="Point Units Type" },
                                new HeaderedContentControl() { Content = lbViewDrawType, Header ="Draw View type" },
                            }
                        }
                    }
                }
            };

            var b = new Binding();
            b.SourcePropertyPath = "PointUnit";
            b.Mode = BindingMode.TwoWay;
            b.Bind(lbPointUnit, ListBox.SelectedItemProperty);
            b = new Binding();
            b.SourcePropertyPath = "ViewDrawType";
            b.Mode = BindingMode.TwoWay;
            b.Bind(lbViewDrawType, ListBox.SelectedItemProperty);

            return window;
        }

        public class PlatformSetupViewModel : ReactiveObject
        {
            public PlatformSetupViewModel()
            {
                ViewDrawTypes = Enum.GetNames(typeof(ViewDrawType));
                PointUnits = Enum.GetNames(typeof(PointUnit));
                var cmd = ReactiveCommand.Create();

                Command = cmd;
                cmd.Subscribe(onNext: o =>
                {
                    var pu = (PointUnit)Enum.Parse(typeof(PointUnit), PointUnit);
                    var vdt = (ViewDrawType)Enum.Parse(typeof(ViewDrawType), ViewDrawType);
                    AndroidPlatform.Instance.DefaultPointUnit = pu;
                    AndroidPlatform.Instance.DefaultViewDrawType = vdt;
                    MainBaseActivity.StartAppFromSetup();
                    //restart app ;
                });
            }

            public string[] ViewDrawTypes { get; set; }
            public string[] PointUnits { get; set; }

            private string _viewDrawType;

            public string ViewDrawType
            {
                get { return _viewDrawType; }
                set { this.RaiseAndSetIfChanged(ref _viewDrawType, value); }
            }

            private string _pointUnit;

            public string PointUnit
            {
                get { return _pointUnit; }
                set { this.RaiseAndSetIfChanged(ref _pointUnit, value); }
            }

            public ICommand Command { get; set; }
        }
    }
}