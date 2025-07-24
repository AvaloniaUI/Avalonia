using System.Collections.Generic;

namespace Avalonia
{
    internal class ElementTreeState
    {
        private Dictionary<Visual, Visual?> _visualParents = new Dictionary<Visual, Visual?>();

        public void SetVisualParent(Visual visual, Visual? parent)
        {
            if (!_visualParents.ContainsKey(visual))
                _visualParents[visual] = parent;
        }

        public Visual? GetVisualParent(Visual? visual)
        {
            if (visual != null && _visualParents.TryGetValue(visual, out var parent))
                return parent;

            return null;
        }

        public void RemoveVisualParent(Visual? visual)
        {
            if(visual != null && _visualParents.ContainsKey(visual))
                _visualParents.Remove(visual);
        }

        public void Clear()
        {
            _visualParents.Clear();
        }
    }
}
