using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.Templates;

public interface ITypedDataTemplate : IDataTemplate
{
    [DataType]
    Type? DataType { get; }
}
