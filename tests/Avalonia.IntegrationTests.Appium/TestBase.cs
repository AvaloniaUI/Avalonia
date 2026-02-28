using System;
using OpenQA.Selenium;
using System.Threading;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

public class TestBase : IDisposable
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

                // If the mouse was captured, the first click might have just released the capture, try again
                if (!page.Selected)
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
    public virtual void Dispose()
    {
        for(var tries=0; tries<3; tries++)
        {
            try
            {
                Assert.NotNull(Session.FindElementByAccessibilityId("Pager"));
                return;
            }
            catch
            {
                Thread.Sleep(3000);
            }
        }
        throw new Exception(
            "===== THE TEST HAS LEFT THE SESSION IN A BROKEN STATE. THE SUBSEQUENT TESTS WILL ALL FAIL =======");
    }
}
