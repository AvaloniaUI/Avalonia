using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public class UiWindowElement : UIElement, IWindowElement
{
    public UiWindowElement(IWebElement inner) : base(inner)
    {
    }

    public string Id => (Inner as AppiumElement).Id;
}
