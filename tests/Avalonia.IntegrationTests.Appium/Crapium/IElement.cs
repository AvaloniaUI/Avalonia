namespace Avalonia.IntegrationTests.Appium.Crapium;

public interface IElement
{
    string Name { get; }
    void Click();
    T FindElement<T>(string id) where T : IElement;
    T FindElementByName<T>(string name) where T : IElement;
}
