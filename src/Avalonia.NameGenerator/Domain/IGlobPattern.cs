namespace Avalonia.NameGenerator.Domain
{
    public interface IGlobPattern
    {
        bool Matches(string str);
    }
}