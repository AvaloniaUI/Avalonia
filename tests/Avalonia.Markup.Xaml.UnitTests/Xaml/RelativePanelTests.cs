using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class RelativePanelTests : XamlTestBase
    {
        [Fact]
        public void ScrollViewer_Viewport_Small_Than_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
        Height='800'
        Width='1000'
>
  <RelativePanel x:Name=""TestRelativePanel"">
    <Panel
          x:Name=""Area1""
          RelativePanel.AlignTopWithPanel=""True""
          RelativePanel.AlignLeftWithPanel=""True""
          RelativePanel.AlignRightWithPanel=""True""
          Height=""100""
          Background=""LightSkyBlue"">
      <TextBlock
          Text=""Area1""
          HorizontalAlignment=""Center""
          VerticalAlignment=""Center""
          />
      <!-- <Button Click=""Button_OnClick"">Second</Button> -->
    </Panel>
    <Panel
      x:Name=""Area2""
      RelativePanel.Below=""Area1""
      RelativePanel.AlignLeftWithPanel=""True""
      RelativePanel.AlignBottomWithPanel=""True""
      Background=""DeepSkyBlue""
        Width=""100""
      >
      <TextBlock
        Text=""Area2""
        HorizontalAlignment=""Center""
        VerticalAlignment=""Center""
        Height=""100""
      />
    </Panel>
    <ScrollViewer
        x:Name=""TestArea""
        Background=""Aqua""
        RelativePanel.Below=""Area1""
        RelativePanel.RightOf=""Area2""
        RelativePanel.AlignRightWithPanel=""True""
        RelativePanel.AlignBottomWithPanel=""True""
        HorizontalScrollBarVisibility=""Visible""
        Margin=""0 0 0 0""
        >
      <ItemsControl
        ItemsSource=""{Binding DataExample}""
        >
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel></StackPanel>
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <StackPanel Orientation=""Horizontal"">
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
            </StackPanel>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </ScrollViewer>
  </RelativePanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                {
                    var panel = window.GetControl<RelativePanel>("TestRelativePanel");

                    panel.DataContext = new
                    {
                        DataExample = Enumerable.Range(1001, 100).Select(e => new { Id = $"{e}" })
                    };
                }
                window.ApplyTemplate();
                window.Show();
                    
                var sv = window.GetControl<ScrollViewer>("TestArea");
                Assert.True(sv.Viewport.Width < sv.Bounds.Width);
                Assert.True(sv.Viewport.Height < sv.Bounds.Height);
            }
        }

    }
}
