global using NUnit.Framework;
global using AvaloniaFactAttribute = Avalonia.Headless.NUnit.AvaloniaTestAttribute;
global using AvaloniaTheoryAttribute = Avalonia.Headless.NUnit.AvaloniaTheoryAttribute;
global using InlineDataAttribute = NUnit.Framework.TestCaseAttribute; 

using Avalonia.Headless;
using Avalonia.Headless.UnitTests;

[assembly: AvaloniaTestApplication(typeof(TestApplication))]
