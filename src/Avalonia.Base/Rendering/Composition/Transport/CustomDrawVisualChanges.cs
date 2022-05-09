namespace Avalonia.Rendering.Composition.Transport
{
    class CustomDrawVisualChanges<TData> : CompositionVisualChanges
    {
        public CustomDrawVisualChanges(IChangeSetPool pool) : base(pool)
        {
        }

        public Change<TData> Data;

        public override void Reset()
        {
            Data.Reset();
            base.Reset();
        }

        public new static ChangeSetPool<CustomDrawVisualChanges<TData>> Pool { get; } =
            new ChangeSetPool<CustomDrawVisualChanges<TData>>(pool => new CustomDrawVisualChanges<TData>(pool));
    }
}