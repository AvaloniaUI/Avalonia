using OpenQA.Selenium;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public class AppiumWindowElement : AppiumElement, IWindowElement
{
    public AppiumWindowElement(IWebElement inner) : base(inner)
    {
    }
}
