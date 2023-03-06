using System;
using System.Collections.Generic;

namespace Avalonia.IntegrationTests.Appium.Wrappers;

public interface ISession
{
    IWindowElement GetWindow(string id);

    IWindowElement GetNewWindow(Action openWindow);

    IEnumerable<IWindowElement> Windows { get; } 
}
