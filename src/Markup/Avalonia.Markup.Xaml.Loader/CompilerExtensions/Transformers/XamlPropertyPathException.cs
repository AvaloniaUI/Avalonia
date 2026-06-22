using System;
using XamlX;
using XamlX.Ast;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    internal class XamlPropertyPathException : XamlTransformException
    {
        public XamlPropertyPathException(string message, IXamlLineInfo lineInfo, Exception? innerException = null)
            : base(message, lineInfo, innerException)
        {
        }
    }
}
