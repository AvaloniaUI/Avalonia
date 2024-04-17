using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters;

/// <summary>
/// Select a valid IDataTemplate in order.
/// </summary>
public class MultiDataTemplatesConverter: IMultiValueConverter
{
    /// <inheritdoc/>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] is IDataTemplate template) return template;
        }
        return null;
    }
}
