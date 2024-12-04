using System;
using System.Runtime.Serialization;

namespace Avalonia.Markup.Xaml
{
    public class XamlLoadException: Exception
    {
        public XamlLoadException()
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051")]
#endif
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
