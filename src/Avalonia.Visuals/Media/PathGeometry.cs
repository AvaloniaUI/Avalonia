using System;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Visuals.Platform;

namespace Avalonia.Media
{
    public class PathGeometry : StreamGeometry
    {
        /// <summary>
        /// Defines the <see cref="Figures"/> property.
        /// </summary>
        public static readonly DirectProperty<PathGeometry, PathFigures> FiguresProperty =
            AvaloniaProperty.RegisterDirect<PathGeometry, PathFigures>(nameof(Figures), g => g.Figures, (g, f) => g.Figures = f);

        /// <summary>
        /// Defines the <see cref="FillRule"/> property.
        /// </summary>
        public static readonly StyledProperty<FillRule> FillRuleProperty =
                                 AvaloniaProperty.Register<PathGeometry, FillRule>(nameof(FillRule));

        private PathFigures _figures;
        private IDisposable _figuresObserver;
        private IDisposable _figuresPropertiesObserver;

        static PathGeometry()
        {
            FiguresProperty.Changed.AddClassHandler<PathGeometry>((s, e) =>
                s.OnFiguresChanged(e.NewValue as PathFigures));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathGeometry"/> class.
        /// </summary>
        public PathGeometry()
        {
            Figures = new PathFigures();
        }

        /// <summary>
        /// Parses the specified path data to a <see cref="PathGeometry"/>.
        /// </summary>
        /// <param name="pathData">The s.</param>
        /// <returns></returns>
        public static new PathGeometry Parse(string pathData)
        {
            var pathGeometry = new PathGeometry();

            using (var context = new PathGeometryContext(pathGeometry))
            using (var parser = new PathMarkupParser(context))
            {
                parser.Parse(pathData);
            }

            return pathGeometry;
        }

        /// <summary>
        /// Gets or sets the figures.
        /// </summary>
        /// <value>
        /// The figures.
        /// </value>
        [Content]
        public PathFigures Figures
        {
            get { return _figures; }
            set { SetAndRaise(FiguresProperty, ref _figures, value); }
        }

        /// <summary>
        /// Gets or sets the fill rule.
        /// </summary>
        /// <value>
        /// The fill rule.
        /// </value>
        public FillRule FillRule
        {
            get { return GetValue(FillRuleProperty); }
            set { SetValue(FillRuleProperty, value); }
        }

        protected override IGeometryImpl CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            var geometry = factory.CreateStreamGeometry();

            using (var ctx = new StreamGeometryContext(geometry.Open()))
            {
                ctx.SetFillRule(FillRule);
                foreach (var f in Figures)
                {
                    f.ApplyTo(ctx);
                }
            }

            return geometry;
        }

        private void OnFiguresChanged(PathFigures figures)
        {
            _figuresObserver?.Dispose();
            _figuresPropertiesObserver?.Dispose();

            _figuresObserver = figures?.ForEachItem(
                _ => InvalidateGeometry(),
                _ => InvalidateGeometry(),
                () => InvalidateGeometry());
            _figuresPropertiesObserver = figures?.TrackItemPropertyChanged(_ => InvalidateGeometry());
        }


        public override string ToString()
            => $"{(FillRule != FillRule.EvenOdd ? "F1 " : "")}{(string.Join(" ", Figures))}";
    }
}
