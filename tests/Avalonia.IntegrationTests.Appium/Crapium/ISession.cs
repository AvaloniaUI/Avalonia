using System.Collections.Generic;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public interface ISession
{
    T FindElement<T>(string windowId, string elementId) where T : IElement;
    
    IWindowElement GetWindow(string id);
    
    IEnumerable<IWindowElement> Windows { get; } 
}
