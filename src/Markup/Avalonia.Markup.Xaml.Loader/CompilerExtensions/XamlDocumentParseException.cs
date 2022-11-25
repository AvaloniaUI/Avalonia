using XamlX;
using XamlX.Ast;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal class XamlDocumentParseException : XamlParseException
{
    public string FilePath { get; }

    public XamlDocumentParseException(string path, XamlParseException parseException)
        : base(parseException.Message, parseException.LineNumber, parseException.LinePosition)
    {
        FilePath = path;
    }
    
    public XamlDocumentParseException(string path, string message, IXamlLineInfo lineInfo)
        : base(message, lineInfo.Line, lineInfo.Position)
    {
        FilePath = path;
    }
}
