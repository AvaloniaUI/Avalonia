using System.Collections.Generic;
using OpenQA.Selenium.Appium.Enums;

namespace Avalonia.IntegrationTests.Appium.Wrappers;

public interface IElement
{
    string Name { get; }
    
    string Text { get; }
    
    bool Enabled { get; }

    string GetAttribute(string attribute);
    
    void Click();

    // TODO eliminate
    void SendClick();

    void SendKeys(string text);

    byte[] GetScreenshot();
    
    IElement FindElementByAccessibilityId(string id);
    
    IElement FindElementByName(string name);

    public IElement FindByXPath(string xpath);

    public IList<IElement> FindManyByXPath(string xpath);

    public IList<IElement> GetChildren();
}
