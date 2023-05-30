using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Animation.Easings;
using Avalonia.Base.UnitTests.Rendering;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositionAnimationParserTests
{
    [Theory,
     InlineData("Vector3(0.5+(4.0-0.5)* -2, 1, 0).X", -6.5)]
    public void EvaluatesExpressionCorrectly(string expression, double value)
    {
        var expr = ExpressionParser.Parse(expression);
        var ctx = new ExpressionEvaluationContext
        {
            ForeignFunctionInterface = BuiltInExpressionFfi.Instance,
        };
        var res = expr.Evaluate(ref ctx);
        double doubleRes;
        if (res.Type == VariantType.Scalar)
            doubleRes = res.Scalar;
        else if (res.Type == VariantType.Double)
            doubleRes = res.Double;
        else
            throw new Exception("Invalid result type: " + res.Type);
        Assert.Equal(value, doubleRes);
    }
}