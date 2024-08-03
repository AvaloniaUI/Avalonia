using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ContextMenuTests : TestBase
    {
        public ContextMenuTests(DefaultAppFixture fixture)
            : base(fixture, "ContextMenu")
        {
        }
        
        [PlatformFact(TestPlatforms.Windows)]
        public void Select_First_Item_With_Down_Arrow_Key()
        {
            var control = Session.FindElementByAccessibilityId("ShowContextMenu");

            new Actions(Session)
                .ContextClick(control)
                .SendKeys(Keys.ArrowDown)
                .Perform();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ContextMenuItem1");
            Assert.True(clickedMenuItem.GetIsFocused());
        }
    }
}
