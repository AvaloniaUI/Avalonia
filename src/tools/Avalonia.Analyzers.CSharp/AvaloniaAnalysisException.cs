using System;
using System.Runtime.Serialization;

namespace Avalonia.Analyzers;

[Serializable]
public class AvaloniaAnalysisException : Exception
{
    public AvaloniaAnalysisException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }

    protected AvaloniaAnalysisException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
