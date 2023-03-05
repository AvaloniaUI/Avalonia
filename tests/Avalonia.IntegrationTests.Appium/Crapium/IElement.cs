namespace Avalonia.IntegrationTests.Appium.Crapium;

public interface IElement
{
    string Name { get; }
    
    string Value { get; }
    
    void Click();

    void SendKeys(string text);
    
    IElement FindElement(string id);
    
    IElement FindElementByName(string name);
}
