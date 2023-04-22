namespace Avalonia.Generators.NameGenerator;

internal enum Options
{
    Public = 0,
    Private = 1,
    Internal = 2,
    Protected = 3,
}

internal enum Behavior
{
    OnlyProperties = 0,
    InitializeComponent = 1,
}

internal enum ViewFileNamingStrategy
{
    ClassName = 0,
    NamespaceAndClassName = 1,
}
