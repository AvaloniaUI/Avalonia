using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.UnitTests.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class XamlIlTests : XamlTestBase
    {
        [Fact]
        public void Transitions_Should_Be_Properly_Parsed()
        {
            var parsed = (Grid)AvaloniaRuntimeXamlLoader.Parse(@"
<Grid xmlns='https://github.com/avaloniaui' >
  <Grid.Transitions>
    <Transitions>
      <DoubleTransition Property='Opacity'
        Easing='CircularEaseIn'
        Duration='0:0:0.5' />
    </Transitions>
  </Grid.Transitions>
</Grid>");
            Assert.Equal(1, parsed.Transitions.Count);
            Assert.Equal(Visual.OpacityProperty, parsed.Transitions[0].Property);
        }

        [Fact]
        public void Parser_Should_Override_Precompiled_Xaml()
        {
            var precompiled = new XamlIlClassWithPrecompiledXaml();
            Assert.Equal(Brushes.Red, precompiled.Background);
            Assert.Equal(1, precompiled.Opacity);
            var loaded = (XamlIlClassWithPrecompiledXaml)AvaloniaRuntimeXamlLoader.Parse(@"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.XamlIlClassWithPrecompiledXaml'
             Opacity='0'>
    
</UserControl>");
            Assert.Equal(loaded.Opacity, 0);
            Assert.Null(loaded.Background);
            
        }

        [Fact]
        public void RelativeSource_TemplatedParent_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                AvaloniaRuntimeXamlLoader.Load(@"
<Application
  xmlns='https://github.com/avaloniaui'
  xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'
>
<Application.Styles>
    <Style Selector='Button'>
      <Setter Property='Template'>
        <ControlTemplate>
          <Grid><Grid><Grid>
            <Canvas>
              <Canvas.Background>
                <SolidColorBrush>
                  <SolidColorBrush.Color>
                    <MultiBinding>
                      <MultiBinding.Converter>
                          <local:XamlIlBugTestsBrushToColorConverter/>
                      </MultiBinding.Converter>
                      <Binding Path='Background' RelativeSource='{RelativeSource TemplatedParent}'/>
                      <Binding Path='Background' RelativeSource='{RelativeSource TemplatedParent}'/>
                      <Binding Path='Background' RelativeSource='{RelativeSource TemplatedParent}'/>
                    </MultiBinding>
                  </SolidColorBrush.Color>
                </SolidColorBrush>
              </Canvas.Background>
            </Canvas>
          </Grid></Grid></Grid>
        </ControlTemplate>
      </Setter>
    </Style>
  </Application.Styles>
</Application>",
                    null, Application.Current); 
                var parsed = (Window)AvaloniaRuntimeXamlLoader.Parse(@"
<Window
  xmlns='https://github.com/avaloniaui'
  xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'
>
  
  <Button Background='Red' />

</Window>
");
                var btn = ((Button)parsed.Content);
                btn.ApplyTemplate();
                var canvas = (Canvas)btn.GetVisualChildren().First()
                    .VisualChildren.First()
                    .VisualChildren.First()
                    .VisualChildren.First();
                Assert.Equal(Brushes.Red.Color, ((ISolidColorBrush)canvas.Background).Color);
            }
        }

        [Fact]
        public void Event_Handlers_Should_Work_For_Templates()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var w =new XamlIlBugTestsEventHandlerCodeBehind();
                w.ApplyTemplate();
                w.Show();

                Dispatcher.UIThread.RunJobs();
                var itemsPresenter = ((ItemsControl)w.Content).GetVisualChildren().FirstOrDefault();
                var item = itemsPresenter
                    .GetVisualChildren().First()
                    .GetVisualChildren().First()
                    .GetVisualChildren().First();

                ((Control)item).DataContext = "test";
                Assert.Equal("test", w.SavedContext);
            }
        }
        
        [Fact]
        public void Custom_Properties_Should_Work_With_XClass()
        {
            var precompiled = new XamlIlClassWithCustomProperty();
            Assert.Equal("123", precompiled.Test);
            var loaded = (XamlIlClassWithCustomProperty)AvaloniaRuntimeXamlLoader.Parse(@"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.XamlIlClassWithCustomProperty'
             Test='321'>

</UserControl>");
            Assert.Equal("321", loaded.Test);
            
        }

        static void AssertThrows(Action callback, Func<Exception, bool> check)
        {
            try
            {
                callback();
            }
            catch (Exception e) when (check(e))
            {
                return;
            }

            throw new Exception("Expected exception was not thrown");
        }
        
        public static object SomeStaticProperty { get; set; }

        [Fact]
        public void Bug2570()
        {
            SomeStaticProperty = "123";
            AssertThrows(() => AvaloniaRuntimeXamlLoader
                    .Load(@"
<UserControl 
    xmlns='https://github.com/avaloniaui'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='clr-namespace:Avalonia.Markup.Xaml.UnitTests'
    d:DataContext='{x:Static tests:XamlIlTests.SomeStaticPropery}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly,
                        designMode: true),
                e => e.Message.Contains("Unable to resolve ")
                     && e.Message.Contains(" as static field, property, constant or enum value"));

        }
        
        [Fact]
        public void Design_Mode_DataContext_Should_Be_Set()
        {
            SomeStaticProperty = "123";
            
            var loaded = (UserControl)AvaloniaRuntimeXamlLoader
                .Load(@"
<UserControl 
    xmlns='https://github.com/avaloniaui'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='clr-namespace:Avalonia.Markup.Xaml.UnitTests'
    d:DataContext='{x:Static tests:XamlIlTests.SomeStaticProperty}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly,
                    designMode: true);
            Assert.Equal(Design.GetDataContext(loaded), SomeStaticProperty);
        }
        
        [Fact]
        public void Attached_Properties_From_Static_Types_Should_Work_In_Style_Setters_Bug_2561()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {

                var parsed = (Window)AvaloniaRuntimeXamlLoader.Parse(@"
<Window
  xmlns='https://github.com/avaloniaui'
  xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'
>
  <Window.Styles>
    <Style Selector='TextBox'>
      <Setter Property='local:XamlIlBugTestsStaticClassWithAttachedProperty.TestInt' Value='100'/>
    </Style>
  </Window.Styles>
  <TextBox/>

</Window>
");
                var tb = ((TextBox)parsed.Content);
                parsed.Show();
                tb.ApplyTemplate();
                Assert.Equal(100, XamlIlBugTestsStaticClassWithAttachedProperty.GetTestInt(tb));
            }
        }

        [Fact]
        public void Provide_Value_Target_Should_Provide_Clr_Property_Info()
        {
            var parsed = AvaloniaRuntimeXamlLoader.Parse<XamlIlClassWithClrPropertyWithValue>(@"
<XamlIlClassWithClrPropertyWithValue 
    xmlns='clr-namespace:Avalonia.Markup.Xaml.UnitTests'
    Count='{XamlIlCheckClrPropertyInfo ExpectedPropertyName=Count}'
/>", typeof(XamlIlClassWithClrPropertyWithValue).Assembly);
            Assert.Equal(6, parsed.Count);
        }

        [Fact]
        public void DataContextType_Resolution()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parsed = AvaloniaRuntimeXamlLoader.Parse<UserControl>(@"
<UserControl 
    xmlns='https://github.com/avaloniaui'
    xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:DataType='local:XamlIlBugTestsDataContext' />");
            }
        }

        [Fact]
        public void DataTemplates_Should_Resolve_Named_Controls_From_Parent_Scope()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parsed = (Window)AvaloniaRuntimeXamlLoader.Parse(@"
<Window
  xmlns='https://github.com/avaloniaui'
  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
>
  <StackPanel>
    <StackPanel.DataTemplates>
      <DataTemplate DataType='{x:Type x:String}'>
       <TextBlock Classes='target' Text='{Binding #txt.Text}'/>
      </DataTemplate>
    </StackPanel.DataTemplates>
    <TextBlock Text='Test' Name='txt'/>
    <ContentControl Content='tst'/>
  </StackPanel>
</Window>
");
                parsed.DataContext = new List<string>() {"Test"};
                parsed.Show();
                parsed.ApplyTemplate();
                var cc = ((ContentControl)((StackPanel)parsed.Content).Children.Last());
                cc.ApplyTemplate();
                var templated = cc.GetVisualDescendants().OfType<TextBlock>()
                    .First(x => x.Classes.Contains("target"));
                Assert.Equal("Test", templated.Text);
            }
        }

        [Fact]
        public void Should_Work_With_Base_Property()
        {
            var parsed = (ListBox)AvaloniaRuntimeXamlLoader.Load(@"
<ListBox
  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
  xmlns='https://github.com/avaloniaui'
  xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'
>
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <ContentControl Content='{Binding}' />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
</ListBox>");

            Assert.NotNull(parsed.ItemTemplate);
        }
        
        [Fact]
        public void Runtime_Loader_Should_Pass_Parents_From_ServiceProvider()
        {
            var sp = new TestServiceProvider
            {
                Parents = new List<object>
                {
                    new UserControl { Resources = { ["Resource1"] = new SolidColorBrush(Colors.Blue) } }
                }
            };
            var document = new RuntimeXamlLoaderDocument(@"
<Button xmlns='https://github.com/avaloniaui' Background='{StaticResource Resource1}' />")
            {
                ServiceProvider = sp
            };
            
            var parsed = (Button)AvaloniaRuntimeXamlLoader.Load(document);
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)parsed.Background!).Color);
        }

        [Fact]
        public void Style_Parser_Throws_For_Duplicate_Setter()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='Width' Value='100'/>
            <Setter Property='Height' Value='20'/>
            <Setter Property='Height' Value='30'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
            var diagnostics = new List<RuntimeXamlDiagnostic>();
            // We still have a runtime check in the StyleInstance class, but in this test we only care about compile warnings.
            Assert.Throws<InvalidOperationException>(() => AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
            {
                LocalAssembly = typeof(XamlIlTests).Assembly,
                DiagnosticHandler = diagnostic =>
                {
                    diagnostics.Add(diagnostic);
                    return diagnostic.Severity;
                }
            }));
            var warning = Assert.Single(diagnostics);
            Assert.Equal(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
            Assert.StartsWith("Duplicate setter encountered for property 'Height'", warning.Title);
        }

        [Fact]
        public void Control_Theme_Parser_Throws_For_Duplicate_Setter()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <ControlTheme x:Key='MyTheme' TargetType='u:TestTemplatedControl'>
            <Setter Property='Width' Value='100'/>
            <Setter Property='Height' Value='20'/>
            <Setter Property='Height' Value='30'/>
        </ControlTheme>
    </Window.Resources>

    <u:TestTemplatedControl Theme='{StaticResource MyTheme}'/>
</Window>";
            var diagnostics = new List<RuntimeXamlDiagnostic>();
            // We still have a runtime check in the StyleInstance class, but in this test we only care about compile warnings.
            Assert.Throws<InvalidOperationException>(() => AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
            {
                LocalAssembly = typeof(XamlIlTests).Assembly,
                DiagnosticHandler = diagnostic =>
                {
                    diagnostics.Add(diagnostic);
                    return diagnostic.Severity;
                }
            }));
            var warning = Assert.Single(diagnostics);
            Assert.Equal(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
            Assert.StartsWith("Duplicate setter encountered for property 'Height'", warning.Title);
        }
    }

    public class XamlIlBugTestsEventHandlerCodeBehind : Window
    {
        public object SavedContext;
        public void HandleDataContextChanged(object sender, EventArgs args)
        {
            SavedContext = ((Control)sender).DataContext;
        }

        public XamlIlBugTestsEventHandlerCodeBehind()
        {
            AvaloniaRuntimeXamlLoader.Load(@"
<Window x:Class='Avalonia.Markup.Xaml.UnitTests.XamlIlBugTestsEventHandlerCodeBehind'
  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
  xmlns='https://github.com/avaloniaui'
  xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'
>
  <ItemsControl>
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <Button DataContextChanged='HandleDataContextChanged' Content='{Binding .}' />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</Window>
", typeof(XamlIlBugTestsEventHandlerCodeBehind).Assembly, this);
            ((ItemsControl)Content).ItemsSource = new[] {"123"};
        }
    }
    
    public class XamlIlClassWithCustomProperty : UserControl
    {
        public string Test { get; set; }

        public XamlIlClassWithCustomProperty()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class XamlIlBugTestsBrushToColorConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            return (values[0] as ISolidColorBrush)?.Color;
        }
    }

    public class XamlIlBugTestsDataContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class XamlIlClassWithPrecompiledXaml : UserControl
    {
    }

    public static class XamlIlBugTestsStaticClassWithAttachedProperty
    {
        public static readonly AvaloniaProperty<int> TestIntProperty = AvaloniaProperty
            .RegisterAttached<Control, int>("TestInt", typeof(XamlIlBugTestsStaticClassWithAttachedProperty));

        public static void SetTestInt(Control control, int value)
        {
            control.SetValue(TestIntProperty, value);
        }

        public static int GetTestInt(Control control)
        {
            return (int)control.GetValue(TestIntProperty);
        }
    }

    public class XamlIlCheckClrPropertyInfoExtension
    {
        public string ExpectedPropertyName { get; set; }

        public object ProvideValue(IServiceProvider prov)
        {
            var pvt = prov.GetService<IProvideValueTarget>();
            var info = (ClrPropertyInfo)pvt.TargetProperty;
            var v = (int)info.Get(pvt.TargetObject);
            return v + 1;
        }
    }

    public class XamlIlClassWithClrPropertyWithValue
    {
        public int Count { get; set; }= 5;
    }
}
