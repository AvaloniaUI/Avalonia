namespace Avalonia.Generators.Domain;

internal interface IGlobPattern
{
    bool Matches(string str);
}