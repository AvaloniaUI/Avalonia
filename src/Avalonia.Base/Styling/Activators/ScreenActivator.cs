using System;
using Avalonia.LogicalTree;
using Avalonia.Platform;

namespace Avalonia.Styling.Activators
{

    internal sealed class OrientationActivator : MediaQueryActivatorBase
    {
        private readonly MediaOrientation _argument;

        public OrientationActivator(Visual visual, MediaOrientation argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && OrientationMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class WidthActivator : MediaQueryActivatorBase
    {
        private readonly (QueryComparisonOperator leftOperator, double left, QueryComparisonOperator rightOperator, double right) _argument;

        public WidthActivator(Visual visual, (QueryComparisonOperator leftOperator, double left, QueryComparisonOperator rightOperator, double right) argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && WidthMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class HeightActivator : MediaQueryActivatorBase
    {
        private readonly (QueryComparisonOperator leftOperator, double left, QueryComparisonOperator rightOperator, double right) _argument;

        public HeightActivator(Visual visual, (QueryComparisonOperator leftOperator, double left, QueryComparisonOperator rightOperator, double right) argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && HeightMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }
}
