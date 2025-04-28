using System;
using System.Collections;
using Avalonia.Styling;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Avalonia.Headless.NUnit;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ThemeVariantValuesAttribute : NUnitAttribute, IParameterDataSource
{
    static readonly object[] data = [ThemeVariant.Light, ThemeVariant.Dark];

    IEnumerable IParameterDataSource.GetData(IParameterInfo parameter)
    {
        if (parameter.ParameterType == typeof(ThemeVariant))
        {
            return data;
        }

        throw new($"Parameter {parameter.ParameterInfo.Name} must be a ThemeVariant to use {nameof(ThemeVariantValuesAttribute)}.");
    }
}
