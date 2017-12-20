using System;
using System.Runtime.Serialization;

namespace Avalonia.Markup.Xaml
{
    public class XamlLoadException: Exception
    {
        public XamlLoadException()
        {
        }

        protected XamlLoadException(SerializationInfo info, StreamingContext context): base(info, context)
        {
        }

        public XamlLoadException(string message): base(message)
        {
        }

        public XamlLoadException(string message, Exception innerException): base(message, innerException)
        {
        }
    }
}