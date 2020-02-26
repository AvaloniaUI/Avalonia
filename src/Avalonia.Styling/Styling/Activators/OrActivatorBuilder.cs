#nullable enable

namespace Avalonia.Styling.Activators
{
    internal struct OrActivatorBuilder
    {
        private IStyleActivator? _single;
        private OrActivator? _multiple;

        public int Count => _multiple?.Count ?? (_single is object ? 1 : 0);

        public void Add(IStyleActivator? activator)
        {
            if (activator == null)
            {
                return;
            }

            if (_single is null && _multiple is null)
            {
                _single = activator;
            }
            else
            {
                if (_multiple is null)
                {
                    _multiple = new OrActivator();
                    _multiple.Add(_single!);
                    _single = null;
                }

                _multiple.Add(activator);
            }
        }

        public IStyleActivator Get() => _single ?? _multiple!;
    }
}
