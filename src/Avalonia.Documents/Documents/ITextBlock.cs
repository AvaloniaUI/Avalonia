using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;

namespace Avalonia.Documents
{
    internal interface ITextBlock : IStyledElement
    {
        string Text { get; set; }

        bool HasComplexContent { get; }
        TextContainer TextContainer { get; }
    }
}
