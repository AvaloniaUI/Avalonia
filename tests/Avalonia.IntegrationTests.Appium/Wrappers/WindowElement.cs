using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Avalonia.IntegrationTests.Appium.Wrappers;

public class WindowElement : Element, IWindowElement, IEquatable<IWindowElement>
{
    private bool _isDisposed;
    private string? _text;
    private string? _name;
    
    public WindowElement(IWebElement inner) : base(inner)
    {
    }

    public override string Name
    {
        get
        {
            if (_name is null)
            {
                _name = base.Name;
            }

            return _name;
        }
    }
    
    public override string Text
    {
        get
        {
            if (_text is null)
            {
                try
                {
                    _text = base.Text;
                }
                catch (Exception)
                {
                    _text = "DeadBeef";
                }
            }

            return _text;
        }
    }

    public string Id => (Inner as AppiumWebElement).Id;

    public void Close()
    {
        var (close, _, _) = this.GetChromeButtons();
        close.Click();
    }
    
    public (IElement close, IElement minimize, IElement maximize) GetChromeButtons()
    {
        var elements = FindManyByXPath("//XCUIElementTypeButton");
        
        return (elements[0], elements[2], elements[1]);
    }

    public bool Equals(IWindowElement? other)
    {
        if (other is WindowElement element)
        {
            return Name == element.Name;
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((WindowElement)obj);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            Close();
        }
    }
}
