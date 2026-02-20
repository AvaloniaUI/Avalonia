using OpenQA.Selenium;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Avalonia.IntegrationTests.Appium;

public class TestBase
{
    protected TestBase(DefaultAppFixture fixture, string pageName)
    {
        Session = fixture.Session;
        TryResetInputState(Session);

        var retry = 0;

        for (;;)
        {
            try
            {
                var pager = Session.FindElementByAccessibilityId("Pager");
                var page = pager.FindElementByName(pageName);
                page.Click();
                Thread.Sleep(100);

                // A second click makes tab activation more reliable when the first click only focuses.
                page.Click();
                Thread.Sleep(100);

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

    private static void TryResetInputState(AppiumDriver session)
    {
        try
        {
            // Appium 1 bindings don't expose ReleaseActions, so send the same command via reflection.
            var execute = session.GetType().GetMethod(
                "Execute",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(Dictionary<string, object>) },
                null);
            execute?.Invoke(session, new object[] { "releaseActions", new Dictionary<string, object>() });
        }
        catch
        {
            // Best effort; continue even if driver doesn't implement it.
        }
    }
}
