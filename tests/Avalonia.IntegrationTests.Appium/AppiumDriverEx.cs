#if APPIUM1
global using AppiumDriver = OpenQA.Selenium.Appium.AppiumDriver<OpenQA.Selenium.Appium.AppiumWebElement>;
global using WindowsDriver = OpenQA.Selenium.Appium.Windows.WindowsDriver<OpenQA.Selenium.Appium.AppiumWebElement>;
global using MacDriver = OpenQA.Selenium.Appium.Mac.MacDriver<OpenQA.Selenium.Appium.AppiumWebElement>;
#elif APPIUM2
global using AppiumWebElement = OpenQA.Selenium.Appium.AppiumElement;
global using AppiumDriver = OpenQA.Selenium.Appium.AppiumDriver;
global using WindowsDriver = OpenQA.Selenium.Appium.Windows.WindowsDriver;
global using MacDriver = OpenQA.Selenium.Appium.Mac.MacDriver;
#endif

using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Internal;

namespace Avalonia.IntegrationTests.Appium;

public static class AppiumDriverEx
{
#if APPIUM2
    public static AppiumElement FindElement(this IFindsElement @this, By by)
    {
        return @this switch
        {
            AppiumDriver driver => driver.FindElement(by),
            AppiumElement driver => driver.FindElement(by),
            _ => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
        };
    }

    public static IReadOnlyList<AppiumWebElement> FindElements(this IFindsElement @this, By by)
    {
        return @this switch
        {
            AppiumDriver driver => driver.FindElements(by),
            AppiumElement driver => driver.FindElements(by),
            _ => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
        };
    }

    public static AppiumWebElement FindElementByAccessibilityId(this IFindsElement session, string criteria) =>
        session.FindElement(MobileBy.AccessibilityId(criteria));

    public static IReadOnlyList<AppiumWebElement> FindElementsByAccessibilityId(this IFindsElement session,
        string criteria) =>
        session.FindElements(MobileBy.AccessibilityId(criteria));

    public static AppiumWebElement FindElementByName(this IFindsElement session, string criteria) =>
        session.FindElement(MobileBy.Name(criteria));

    public static IReadOnlyList<AppiumWebElement> FindElementsByName(this IFindsElement session, string criteria) =>
        session.FindElements(MobileBy.Name(criteria));

    public static AppiumWebElement FindElementByXPath(this IFindsElement session, string criteria) =>
        session.FindElement(By.XPath(criteria));

    public static IReadOnlyList<AppiumWebElement> FindElementsByXPath(this IFindsElement session, string criteria) =>
        session.FindElements(By.XPath(criteria));

    public static AppiumWebElement FindElementByClassName(this IFindsElement session, string criteria) =>
        session.FindElement(By.ClassName(criteria));

    public static IReadOnlyList<AppiumWebElement> FindElementsByClassName(this IFindsElement session, string criteria) =>
        session.FindElements(By.ClassName(criteria));

    public static AppiumWebElement FindElementByTagName(this IFindsElement session, string criteria) =>
        session.FindElement(By.TagName(criteria));

    public static IReadOnlyList<AppiumWebElement> FindElementsByTagName(this IFindsElement session, string criteria) =>
        session.FindElements(By.TagName(criteria));

    public static void AddAdditionalCapability(this AppiumOptions options, string name, object value)
    {
        if (name == MobileCapabilityType.AutomationName)
        {
            options.AutomationName = value.ToString();
        }
        else if (name == MobileCapabilityType.PlatformName)
        {
            options.PlatformName = value.ToString();
        }
        else
        {
            options.AddAdditionalAppiumOption(name, value);
        }
    }
#endif

    public static Actions MoveToElementCenter(this Actions actions, AppiumWebElement element, int xOffset, int yOffset)
    {
#if APPIUM2
        // It's always Center in Appium 2
        return actions.MoveToElement(element, xOffset, yOffset);
#else
        return actions.MoveToElement(element, xOffset, yOffset, MoveToElementOffsetOrigin.Center);
#endif
    }
}
