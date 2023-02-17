using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Transport;


// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Animations
{
    public class CompositionAnimationGroup : CompositionObject, ICompositionAnimationBase
    {
        internal List<CompositionAnimation> Animations { get; } = new List<CompositionAnimation>();
        void ICompositionAnimationBase.InternalOnly()
        {
            
        }

        public void Add(CompositionAnimation value) => Animations.Add(value);
        public void Remove(CompositionAnimation value) => Animations.Remove(value);
        public void RemoveAll() => Animations.Clear();

        public CompositionAnimationGroup(Compositor compositor) : base(compositor, null)
        {
        }
    }
}
