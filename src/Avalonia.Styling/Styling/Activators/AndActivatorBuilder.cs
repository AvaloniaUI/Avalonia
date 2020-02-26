#nullable enable

namespace Avalonia.Styling.Activators
{
    internal struct AndActivatorBuilder
    {
        private IStyleActivator? _single;
        private AndActivator? _multiple;

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
                    _multiple = new AndActivator();
                    _multiple.Add(_single!);
                    _single = null;
                }

                _multiple.Add(activator);
            }
        }

        public IStyleActivator Get() => _single ?? _multiple!;
    }
}
