using System;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

/// <summary>
/// These are tested locally by developer and passed. Disabled to avoid calling Process.Start in CI/CD
/// </summary>
public class HyperlinkButtonTests
{
    private class TestHyperlinkButton : HyperlinkButton
    {
        public new void OnClick()
        {
            base.OnClick();
        }
    }
    
    private MouseTestHelper _helper = new MouseTestHelper();
    
    [Fact]
    public void HyperlinkButton_IsVisitedSetOnClick_Enabled()
    {
        var hyperlinkButton = new TestHyperlinkButton()
        {
            IsVisitedSetOnClick = true, 
            NavigateUri = new Uri("test://test"),
        };
        // hyperlinkButton.OnClick();
        // Assert.True(hyperlinkButton.IsVisited);
        // Assert.True(hyperlinkButton.Classes.Contains(HyperlinkButton.pcVisited));
    }
    
    [Fact]
    public void HyperlinkButton_Reset_IsVisited()
    {
        var hyperlinkButton = new TestHyperlinkButton()
        {
            IsVisitedSetOnClick = true, 
            NavigateUri = new Uri("test://test"),
        };
        // hyperlinkButton.OnClick();
        // Assert.True(hyperlinkButton.IsVisited);
        // Assert.True(hyperlinkButton.Classes.Contains(HyperlinkButton.pcVisited));

        // hyperlinkButton.IsVisited = false;
        // Assert.False(hyperlinkButton.IsVisited);
        // Assert.False(hyperlinkButton.Classes.Contains(HyperlinkButton.pcVisited));
        
        // hyperlinkButton.OnClick();
        // Assert.True(hyperlinkButton.IsVisited);
        // Assert.True(hyperlinkButton.Classes.Contains(HyperlinkButton.pcVisited));
    }
    
    [Fact]
    public void HyperlinkButton_IsVisitedSetOnClick_Disabled()
    {
        var hyperlinkButton = new TestHyperlinkButton()
        {
            IsVisitedSetOnClick = false, 
            NavigateUri = new Uri("test://test"),
        };
        // hyperlinkButton.OnClick();
        // Assert.False(hyperlinkButton.IsVisited);
        // Assert.False(hyperlinkButton.Classes.Contains(HyperlinkButton.pcVisited));
    }
    
    [Fact]
    public void HyperlinkButton_Empty_NavigationUri()
    {
        var hyperlinkButton = new TestHyperlinkButton()
        {
            IsVisitedSetOnClick = true, 
            NavigateUri = null,
        };
        // hyperlinkButton.OnClick();
        // Assert.False(hyperlinkButton.IsVisited);
        // Assert.False(hyperlinkButton.Classes.Contains(HyperlinkButton.pcVisited));
    }
}
