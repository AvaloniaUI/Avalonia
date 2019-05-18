using System;
using System.Xml;
using Portable.Xaml;

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
                if(e is XamlObjectWriterException || e is XmlException)
                    return;
            }
            throw new Exception("Expected to throw xaml exception");
        }
    }
}
