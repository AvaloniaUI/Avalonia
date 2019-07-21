using System;
using System.Xml;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class XamlTestHelpers
    {
        public static void AssertThrowsXamlException(Action cb)
        {
            try
            {
                cb();
            }
            catch (Exception e)
            {
                if(e is XmlException)
                    return;
            }
            throw new Exception("Expected to throw xaml exception");
        }
    }
}
