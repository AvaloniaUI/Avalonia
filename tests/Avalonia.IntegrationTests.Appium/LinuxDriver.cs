using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Remote;

namespace Avalonia.IntegrationTests.Appium;

#if APPIUM1
public class LinuxElement : AppiumWebElement
{
    public LinuxElement(RemoteWebDriver parent, string id)
        : base(parent, id)
    {
    }
}

internal class LinuxElementFactory : CachedElementFactory<LinuxElement>
{
    public LinuxElementFactory(RemoteWebDriver parentDriver)
        : base(parentDriver)
    {
    }

    protected override LinuxElement CreateCachedElement(RemoteWebDriver parentDriver, string elementId) =>
        new(parentDriver, elementId);
}

/// <summary>
/// A concrete AppiumDriver subclass for connecting to the KDE selenium-webdriver-at-spi
/// WebDriver server on Linux.
/// </summary>
public class LinuxDriver<W> : AppiumDriver<W> where W : IWebElement
{
    public LinuxDriver(Uri remoteAddress, AppiumOptions options)
        : base(remoteAddress, options.ToCapabilities())
    {
    }

    public LinuxDriver(Uri remoteAddress, AppiumOptions options, TimeSpan commandTimeout)
        : base(remoteAddress, options.ToCapabilities(), commandTimeout)
    {
    }

    protected override RemoteWebElementFactory CreateElementFactory() =>
        new LinuxElementFactory(this);
}
#elif APPIUM2
/// <summary>
/// A concrete AppiumDriver subclass for connecting to the KDE selenium-webdriver-at-spi
/// WebDriver server on Linux.
/// </summary>
public class LinuxDriver : AppiumDriver
{
    public LinuxDriver(Uri remoteAddress, AppiumOptions options)
        : base(remoteAddress, options.ToCapabilities())
    {
    }

    public LinuxDriver(Uri remoteAddress, AppiumOptions options, TimeSpan commandTimeout)
        : base(remoteAddress, options.ToCapabilities(), commandTimeout)
    {
    }
}
#endif
