using Avalonia.Layout;

namespace Avalonia.Styling.Activators
{
    internal sealed class WidthActivator : ContainerQueryActivatorBase
    {
        private readonly (StyleQueryComparisonOperator @operator, double value) _argument;

        public WidthActivator(Visual visual, (StyleQueryComparisonOperator @operator, double value) argument, string? containerName = null) : base(visual, containerName)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => (CurrentContainer is Layoutable layoutable
            && Container.GetSizing(layoutable) is { } sizing
            && Container.GetQueryProvider(layoutable) is { } queryProvider

            && (sizing is Styling.ContainerSizing.Width or Styling.ContainerSizing.WidthAndHeight))
            && WidthQuery.Evaluate(queryProvider, _argument).IsMatch;
    }

    internal sealed class HeightActivator : ContainerQueryActivatorBase
    {
        private readonly (StyleQueryComparisonOperator @operator, double value) _argument;

        public HeightActivator(Visual visual, (StyleQueryComparisonOperator @operator, double value) argument, string? containerName = null) : base(visual, containerName)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => (CurrentContainer is Layoutable layoutable
            && Container.GetSizing(layoutable) is { } sizing
            && Container.GetQueryProvider(layoutable) is { } queryProvider
            && (sizing is Styling.ContainerSizing.Height or Styling.ContainerSizing.WidthAndHeight))
            && HeightQuery.Evaluate(queryProvider, _argument).IsMatch;
    }
}
