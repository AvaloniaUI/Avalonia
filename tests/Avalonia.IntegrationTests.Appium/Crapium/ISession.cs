using System.Collections.Generic;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public interface ISession
{
    IWindowElement GetWindow(string id);
    
    IEnumerable<IWindowElement> Windows { get; } 
}
