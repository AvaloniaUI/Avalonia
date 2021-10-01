namespace Avalonia.Controls.Templates
{
    public class TemplateResult<T>
    {
        public T Result { get; }
        public INameScope NameScope { get; }

        public TemplateResult(T result, INameScope nameScope)
        {
            Result = result;
            NameScope = nameScope;
        }

        public void Deconstruct(out T result, out INameScope scope)
        {
            result = Result;
            scope = NameScope;
        }
    }
}
