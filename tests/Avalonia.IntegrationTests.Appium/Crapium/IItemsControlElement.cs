namespace Avalonia.IntegrationTests.Appium.Crapium;

public interface IItemsControlElement : IElement
{
    T FindItemByName<T>(string name) where T : IElement;
}
