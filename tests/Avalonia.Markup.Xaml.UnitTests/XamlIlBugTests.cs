using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media;
using JetBrains.Annotations;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class XamlIlBugTests
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

}
