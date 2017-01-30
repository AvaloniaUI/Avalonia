using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public enum ExpandDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    public class Expander : HeaderedContentControl
    {
        public static readonly DirectProperty<Expander, IPageTransition> ContentTransitionProperty =
            AvaloniaProperty.RegisterDirect<Expander, IPageTransition>(
                nameof(ContentTransition),
                o => o.ContentTransition,
                (o, v) => o.ContentTransition = v);

        public static readonly DirectProperty<Expander, ExpandDirection> ExpandDirectionProperty =
            AvaloniaProperty.RegisterDirect<Expander, ExpandDirection>(
                nameof(ExpandDirection),
                o => o.ExpandDirection,
                (o, v) => o.ExpandDirection = v,
                ExpandDirection.Down);

        public static readonly DirectProperty<Expander, bool> IsExpandedProperty =
            AvaloniaProperty.RegisterDirect<Expander, bool>(
                nameof(IsExpanded),
                o => o.IsExpanded,
                (o, v) => o.IsExpanded = v,
                defaultBindingMode: Data.BindingMode.TwoWay);

        static Expander()
        {
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Down, ":down");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Up, ":up");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Left, ":left");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Right, ":right");

            PseudoClass(IsExpandedProperty, ":expanded");

            IsExpandedProperty.Changed.AddClassHandler<Expander>(x => x.OnIsExpandedChanged);
        }

        public IPageTransition ContentTransition
        {
            get { return _contentTransition; }
            set { SetAndRaise(ContentTransitionProperty, ref _contentTransition, value); }
        }

        public ExpandDirection ExpandDirection
        {
            get { return _expandDirection; }
            set { SetAndRaise(ExpandDirectionProperty, ref _expandDirection, value); }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetAndRaise(IsExpandedProperty, ref _isExpanded, value); }
        }

        protected virtual void OnIsExpandedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            IVisual visualContent = Presenter;

            if (Content != null && ContentTransition != null && visualContent != null)
            {
                bool forward = ExpandDirection == ExpandDirection.Left ||
                                ExpandDirection == ExpandDirection.Up;
                if (IsExpanded)
                {
                    ContentTransition.Start(null, visualContent, forward);
                }
                else
                {
                    ContentTransition.Start(visualContent, null, !forward);
                }
            }
        }

        private IPageTransition _contentTransition;
        private ExpandDirection _expandDirection;
        private bool _isExpanded;
    }
}