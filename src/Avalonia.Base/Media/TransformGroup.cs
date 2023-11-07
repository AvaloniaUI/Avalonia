using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    public sealed class TransformGroup : Transform
    {
        private Matrix? _lastMatrix;
        /// <summary>
        /// Defines the <see cref="Children"/> property.
        /// </summary>
        public static readonly StyledProperty<Transforms> ChildrenProperty =
            AvaloniaProperty.Register<TransformGroup, Transforms>(nameof(Children));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012",
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public TransformGroup()
        {
            Children = new Transforms();
            Children.ResetBehavior = ResetBehavior.Remove;
            Children.CollectionChanged += delegate
            {
                Children.ForEachItem(
                    (tr) => tr.Changed += ChildTransform_Changed,
                    (tr) => tr.Changed -= ChildTransform_Changed,
                    () => { });
            };
        }

        private void ChildTransform_Changed(object? sender, System.EventArgs e)
        {
            _lastMatrix = null;
            this.RaiseChanged();
        }

        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        [Content]
        public Transforms Children
        {
            get { return GetValue(ChildrenProperty); }
            set { SetValue(ChildrenProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix" />.
        /// </summary>
        public override Matrix Value
        {
            get
            {
                if (_lastMatrix is null)
                {
                    var matrix = Matrix.Identity;
                    foreach (var t in Children)
                    {
                        matrix *= t.Value;
                    }
                    _lastMatrix = matrix;
                }
                return _lastMatrix.Value;
            }
        }
    }

    public sealed class Transforms : AvaloniaList<Transform>
    {
    }
}
