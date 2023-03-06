using System;

namespace Avalonia.IntegrationTests.Appium.Wrappers;

public interface IWindowElement : IElement, IDisposable
{
    public void Close();
}
