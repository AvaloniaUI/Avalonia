using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using JetBrains.Annotations;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class XamlIlTests
    {
        [Fact]
        public void Binding_Button_IsPressed_ShouldWork()
        {
            var parsed = (Button)AvaloniaXamlLoader.Parse(@"
<Button xmlns='https://github.com/avaloniaui' IsPressed='{Binding IsPressed, Mode=TwoWay}' />");
            var ctx = new XamlIlBugTestsDataContext();
            parsed.DataContext = ctx;
            parsed.SetValue(Button.IsPressedProperty, true);
            Assert.True(ctx.IsPressed);
        }

        [Fact]
        public void Transitions_Should_Be_Properly_Parsed()
        {
            var parsed = (Grid)AvaloniaXamlLoader.Parse(@"
<Grid xmlns='https://github.com/avaloniaui' >
  <Grid.Transitions>
    <DoubleTransition Property='Opacity'
       Easing='CircularEaseIn'
       Duration='0:0:0.5' />
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
            var loaded = (XamlIlClassWithPrecompiledXaml)AvaloniaXamlLoader.Parse(@"
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
                new AvaloniaXamlLoader().Load(@"
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
                var parsed = (Window)AvaloniaXamlLoader.Parse(@"
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
            var loaded = (XamlIlClassWithCustomProperty)AvaloniaXamlLoader.Parse(@"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Avalonia.Markup.Xaml.UnitTests.XamlIlClassWithCustomProperty'
             Test='321'>

</UserControl>");
            Assert.Equal("321", loaded.Test);
            
        }

        void AssertThrows(Action callback, Func<Exception, bool> check)
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
            AssertThrows(() => new AvaloniaXamlLoader() {IsDesignMode = true}
                    .Load(@"
<UserControl 
    xmlns='https://github.com/avaloniaui'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='clr-namespace:Avalonia.Markup.Xaml.UnitTests'
    d:DataContext='{x:Static tests:XamlIlTests.SomeStaticPropery}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly),
                e => e.Message.Contains("Unable to resolve ")
                     && e.Message.Contains(" as static field, property, constant or enum value"));

        }
        
        [Fact]
        public void Design_Mode_DataContext_Should_Be_Set()
        {
            SomeStaticProperty = "123";
            
            var loaded = (UserControl)new AvaloniaXamlLoader() {IsDesignMode = true}
                .Load(@"
<UserControl 
    xmlns='https://github.com/avaloniaui'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='clr-namespace:Avalonia.Markup.Xaml.UnitTests'
    d:DataContext='{x:Static tests:XamlIlTests.SomeStaticProperty}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly);
            Assert.Equal(Design.GetDataContext(loaded), SomeStaticProperty);
        }
        
        [Fact]
        public void Attached_Properties_From_Static_Types_Should_Work_In_Style_Setters_Bug_2561()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {

                var parsed = (Window)AvaloniaXamlLoader.Parse(@"
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
            var parsed = AvaloniaXamlLoader.Parse<XamlIlClassWithClrPropertyWithValue>(@"
<XamlIlClassWithClrPropertyWithValue 
    xmlns='clr-namespace:Avalonia.Markup.Xaml.UnitTests'
    Count='{XamlIlCheckClrPropertyInfo ExpectedPropertyName=Count}'
/>", typeof(XamlIlClassWithClrPropertyWithValue).Assembly);
            Assert.Equal(6, parsed.Count);
        }

        [Fact]
        public void Should_Provide_PropertyPath_For_Setters()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parsed = AvaloniaXamlLoader.Parse<Style>(@"
<Style Selector='Animatable' xmlns='https://github.com/avaloniaui'>
    <Setter PropertyPath=':>Visual.Bounds.BottomRight.X' Value='0' />
    <Setter PropertyPath=':>Visual.RenderTransform:=ScaleTransform.ScaleX' Value='0' />
    <Setter PropertyPath='(Visual.RenderTransform):=ScaleTransform.ScaleX' Value='0' />
</Style>");
                var s1e = ((Setter)parsed.Setters[0]).PropertyPath.Elements;
                var s2e = ((Setter)parsed.Setters[1]).PropertyPath.Elements;
                var s3e = ((Setter)parsed.Setters[2]).PropertyPath.Elements;
                
                Assert.Equal(typeof(Visual), ((CastTypePropertyPathElement)s1e[0]).Type);
                Assert.IsType<ChildTraversalPropertyPathElement>(s1e[1]);
                Assert.Equal("Bounds", ((AvaloniaProperty)((PropertyPropertyPathElement)s1e[2]).Property).Name);
                Assert.IsType<ChildTraversalPropertyPathElement>(s1e[3]);
                var bottomRight = ((PropertyPropertyPathElement)s1e[4]).Property;
                Assert.IsType<ChildTraversalPropertyPathElement>(s1e[5]);
                var pointX = ((PropertyPropertyPathElement)s1e[6]).Property;
                
                var orect = (object)(new Rect(100, 100, 200, 200));
                var point = bottomRight.Get(orect);
                var x = pointX.Get(point);
                Assert.Equal(300, (double)x);
                
                Assert.Equal(typeof(Visual), ((CastTypePropertyPathElement)s2e[0]).Type);
                Assert.IsType<ChildTraversalPropertyPathElement>(s2e[1]);
                Assert.Equal("RenderTransform", ((AvaloniaProperty)((PropertyPropertyPathElement)s2e[2]).Property).Name);
                Assert.Equal(typeof(ScaleTransform), ((EnsureTypePropertyPathElement)s2e[3]).Type);
                Assert.IsType<ChildTraversalPropertyPathElement>(s2e[4]);
                Assert.Equal("ScaleX", ((AvaloniaProperty)((PropertyPropertyPathElement)s2e[5]).Property).Name);


                var s3fqp = (AvaloniaProperty)((PropertyPropertyPathElement)s3e[0]).Property;
                Assert.Equal("RenderTransform", s3fqp.Name);
                Assert.Equal(typeof(Visual), s3fqp.OwnerType);
                Assert.Equal(typeof(ScaleTransform), ((EnsureTypePropertyPathElement)s3e[1]).Type);
                Assert.IsType<ChildTraversalPropertyPathElement>(s3e[2]);
                Assert.Equal("ScaleX", ((AvaloniaProperty)((PropertyPropertyPathElement)s3e[3]).Property).Name);
                
                
            }
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
            new AvaloniaXamlLoader().Load(@"
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
            ((ItemsControl)Content).Items = new[] {"123"};
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
        public bool IsPressed { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
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
