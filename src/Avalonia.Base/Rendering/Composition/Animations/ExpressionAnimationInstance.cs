using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;


namespace Avalonia.Rendering.Composition.Animations
{
    
    /// <summary>
    /// Server-side counterpart of <see cref="ExpressionAnimation"/> with values baked-in.
    /// </summary>
    internal class ExpressionAnimationInstance : AnimationInstanceBase, IAnimationInstance
    {
        private readonly Expression _expression;
        private ExpressionVariant _startingValue;
        private readonly ExpressionVariant? _finalValue;

        protected override ExpressionVariant EvaluateCore(TimeSpan now, ExpressionVariant currentValue)
        {
            var ctx = new ExpressionEvaluationContext
            {
                Parameters = Parameters,
                Target = TargetObject,
                ForeignFunctionInterface = BuiltInExpressionFfi.Instance,
                StartingValue = _startingValue,
                FinalValue = _finalValue ?? _startingValue,
                CurrentValue = currentValue
            };
            return _expression.Evaluate(ref ctx);
        }

        public override void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, CompositionProperty property)
        {
            _startingValue = startingValue;
            var hs = new HashSet<(string, string)>();
            _expression.CollectReferences(hs);
            base.Initialize(property, hs);
        }
        
        public ExpressionAnimationInstance(Expression expression,
            ServerObject target,
            ExpressionVariant? finalValue,
            PropertySetSnapshot parameters) : base(target, parameters)
        {
            _expression = expression;
            _finalValue = finalValue;
        }
    }
}
