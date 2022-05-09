using System;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations
{
    internal class ExpressionAnimationInstance : IAnimationInstance
    {
        private readonly Expression _expression;
        private readonly IExpressionObject _target;
        private ExpressionVariant _startingValue;
        private readonly ExpressionVariant? _finalValue;
        private readonly PropertySetSnapshot _parameters;

        public ExpressionVariant Evaluate(TimeSpan now, ExpressionVariant currentValue)
        {
            var ctx = new ExpressionEvaluationContext
            {
                Parameters = _parameters,
                Target = _target,
                ForeignFunctionInterface = BuiltInExpressionFfi.Instance,
                StartingValue = _startingValue,
                FinalValue = _finalValue ?? _startingValue,
                CurrentValue = currentValue
            };
            return _expression.Evaluate(ref ctx);
        }

        public void Start(TimeSpan startedAt, ExpressionVariant startingValue)
        {
            _startingValue = startingValue;
        }

        public ExpressionAnimationInstance(Expression expression,
            IExpressionObject target,
            ExpressionVariant? finalValue,
            PropertySetSnapshot parameters)
        {
            _expression = expression;
            _target = target;
            _finalValue = finalValue;
            _parameters = parameters;
        }
    }
}