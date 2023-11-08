using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class DependsOnAttributeTest
{
    const string Case1_Xaml = @"<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:p='clr-namespace:Avalonia.Markup.Xaml.UnitTests'>
   <p:DependsOnControlTest x:Name='target' First='1' Second='2' />
</Window>";

    const string Case2_Xaml = @"<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:p='clr-namespace:Avalonia.Markup.Xaml.UnitTests'>
   <p:DependsOnControlTest x:Name='target' Second='2' First='1'  />
</Window>";

    [Theory]
    [InlineData(Case1_Xaml)]
    [InlineData(Case2_Xaml)]
    public void Ensure_The_Initialization_Sequence_Of_Properties(string xaml)
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            try
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.Find<DependsOnControlTest>("target");

                Assert.NotNull(target);

                Assert.Equal(5, target.Changes.Count);
                Assert.Equal(nameof(DependsOnControlTest.First), target.Changes[1].Property);
                Assert.Equal(nameof(DependsOnControlTest.Second), target.Changes[2].Property);

            }
            catch (System.Exception ex)
            {
                var inner = ex.InnerException;
                if (inner is null)
                {
                    throw;
                }
                var last = inner;
                while (inner is not null)
                {
                    last = inner;
                    inner = inner.InnerException;
                }
                throw last;
            }
        }
    }

    
}
