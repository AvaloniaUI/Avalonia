using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Collections
{
    public class SafeEnumerateCollectionTests
    {
        private class ViewModel1
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
        }

        private class ViewModel2
        {
            public string CarProducer { get; set; } = "";
            public string CarModel { get; set; } = "";
        }

        [Fact]
        public void NoExceptionWhenDetachingTabControlWithTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                TabControl tabControl;
                IDataTemplate contentTemplate = new FuncDataTemplate<object>(
                    (i, ns) =>
                    {
                        return
                        new Border()
                        {
                            BorderBrush = Brushes.Red,
                            BorderThickness = new Thickness(4),
                            Padding = new Thickness(3),
                            Child = new ContentControl
                            {
                                Content = i,
                            }
                        };
                    });
            
                var window = new Window
                {
                    Content = tabControl = new TabControl
                    {
                        ItemsSource = new object[]
                        {
                            new ViewModel1
                            {
                                FirstName = "Vasily",
                                LastName = "Pupkin",
                            },
                            new ViewModel2
                            {
                                CarProducer = "Fiat",
                                CarModel = "Uno",
                            },
                        },
                        SelectedIndex = 0,
                    },
                    Styles=
                    {
                        new Style(x => x.Is<TabItem>())
                        {
                            Setters =
                            {
                                new Setter{ Property = TabItem.ContentTemplateProperty,
                                Value = contentTemplate,
                                }
                            }
                        }
                    },
                    Width = 640,
                    Height = 480,
                };
                window.Show();
                window.Close();
            }
        }
    }
}
