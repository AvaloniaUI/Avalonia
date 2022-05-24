namespace Avalonia.SourceGenerator.CompositionGenerator;

public interface ICompositionGeneratorSink
{
    void AddSource(string name, string code);
}