using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.UnitTests.Helpers;

static class ScopedSanityCheck
{
    [ModuleInitializer]
    public static void SanityCheck()
    {
        var offendingTypes = new List<string>();
        void CheckRecursive(Type type)
        {
            if (type.GetMethods().Any(m => m.GetCustomAttributes(true).Any(a => a is FactAttribute or TheoryAttribute)))
            {
                if (!typeof(ScopedTestBase).IsAssignableFrom(type))
                    offendingTypes.Add(type.ToString());
            }

            foreach (var t in type.GetNestedTypes())
                CheckRecursive(t);
        }
        
        foreach(var t in typeof(ScopedSanityCheck).Assembly.GetTypes())
            CheckRecursive(t);

        if (offendingTypes.Count > 0)
            throw new Exception(
                $"Test types:\n{string.Join("\n", offendingTypes.ToArray())}\n don't inherit from ScopedTestBase");
    }
}