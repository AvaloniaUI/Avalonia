using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
                ((Control)item).RaiseEvent(new PointerPressedEventArgs {ClickCount = 20});
                Assert.Equal(20, w.Args.ClickCount);
            }
        }
    }
    
    public class XamlIlBugTestsEventHandlerCodeBehind : Window
    {
        public PointerPressedEventArgs Args;
        public void HandlePointerPressed(object sender, PointerPressedEventArgs args)
        {
            Args = args;
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
        <Button PointerPressed='HandlePointerPressed' Content='{Binding .}' />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</Window>
", typeof(XamlIlBugTestsEventHandlerCodeBehind).Assembly, this);
            ((ItemsControl)Content).Items = new[] {"123"};
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

}
