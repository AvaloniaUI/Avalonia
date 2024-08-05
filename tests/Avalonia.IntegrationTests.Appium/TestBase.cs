using OpenQA.Selenium;
using System.Threading;

namespace Avalonia.IntegrationTests.Appium;

public class TestBase
{
    protected TestBase(DefaultAppFixture fixture, string pageName)
    {
        Session = fixture.Session;

        var retry = 0;

        for (;;)
        {
            try
            {
                var pager = Session.FindElementByAccessibilityId("Pager");
                var page = pager.FindElementByName(pageName);
                page.Click();
                break;
            }
            catch (WebDriverException) when (retry++ < 3)
            {
                // MacOS sometimes seems to need a bit of time to get itself back in order after switching out
                // of fullscreen.
                Thread.Sleep(1000);
            }
        }
    }

    protected AppiumDriver Session { get; }
}
