namespace Avalonia.Controls.Templates
{
    public interface ITemplateResult
    {
        public object? Result { get; }
        public INameScope NameScope { get; }
    }
}
