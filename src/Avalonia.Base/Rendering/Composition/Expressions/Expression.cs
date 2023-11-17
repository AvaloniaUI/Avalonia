using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Expressions
{
    /// <summary>
    /// A parsed composition expression
    /// </summary>
    internal abstract class Expression
    {
        public abstract ExpressionType Type { get; }
        public static Expression Parse(string expression)
        {
            return ExpressionParser.Parse(expression.AsSpan());
        }

        public abstract ExpressionVariant Evaluate(ref ExpressionEvaluationContext context);

        public virtual void CollectReferences(HashSet<(string parameter, string property)> references)
        {
            
        }

        protected abstract string Print();
        public override string ToString() => Print();

        internal static string OperatorName(ExpressionType t)
        {
            var attr = typeof(ExpressionType).GetMember(t.ToString())[0]
                .GetCustomAttribute<PrettyPrintStringAttribute>();
            if (attr != null)
                return attr.Name;
            return t.ToString();
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class PrettyPrintStringAttribute : Attribute
    {
        public string Name { get; }

        public PrettyPrintStringAttribute(string name)
        {
            Name = name;
        }
    }
    
    internal enum ExpressionType
    {
        // Binary operators
        [PrettyPrintString("+")]
        Add,
        [PrettyPrintString("-")]
        Subtract,
        [PrettyPrintString("/")]
        Divide,
        [PrettyPrintString("*")]
        Multiply,
        [PrettyPrintString(">")]
        MoreThan,
        [PrettyPrintString("<")]
        LessThan,
        [PrettyPrintString(">=")]
        MoreThanOrEqual,
        [PrettyPrintString("<=")]
        LessThanOrEqual,
        [PrettyPrintString("&&")]
        LogicalAnd,
        [PrettyPrintString("||")]
        LogicalOr,
        [PrettyPrintString("%")]
        Remainder,
        [PrettyPrintString("==")]
        Equals,
        [PrettyPrintString("!=")]
        NotEquals,
        // Unary operators
        [PrettyPrintString("!")]
        Not,
        [PrettyPrintString("-")]
        UnaryMinus,
        // The rest
        MemberAccess,
        Parameter,
        FunctionCall,
        Keyword,
        Constant,
        ConditionalExpression
    }

    internal enum ExpressionKeyword
    {
        StartingValue,
        CurrentValue,
        FinalValue,
        Target,
        Pi,
        True,
        False
    }

    internal class ConditionalExpression : Expression
    {
        public Expression Condition { get; }
        public Expression TruePart { get; }
        public Expression FalsePart { get; }
        public override ExpressionType Type => ExpressionType.ConditionalExpression;

        public ConditionalExpression(Expression condition, Expression truePart, Expression falsePart)
        {
            Condition = condition;
            TruePart = truePart;
            FalsePart = falsePart;
        }
        
        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
        {
            var cond = Condition.Evaluate(ref context);
            if (cond.Type == VariantType.Boolean && cond.Boolean)
                return TruePart.Evaluate(ref context);
            return FalsePart.Evaluate(ref context);
        }

        public override void CollectReferences(HashSet<(string parameter, string property)> references)
        {
            Condition.CollectReferences(references);
            TruePart.CollectReferences(references);
            FalsePart.CollectReferences(references);
        }

        protected override string Print() => $"({Condition}) ? ({TruePart}) : ({FalsePart})";
    }
    
    internal class ConstantExpression : Expression
    {
        public float Constant { get; }
        public override ExpressionType Type => ExpressionType.Constant;

        public ConstantExpression(float constant)
        {
            Constant = constant;
        }

        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context) => Constant;

        protected override string Print() => Constant.ToString(CultureInfo.InvariantCulture);
    }

    internal class FunctionCallExpression : Expression
    {
        public string Name { get; }
        public List<Expression> Parameters { get; }
        public override ExpressionType Type => ExpressionType.FunctionCall;

        public FunctionCallExpression(string name, List<Expression> parameters)
        {
            Name = name;
            Parameters = parameters;
        }
        
        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
        {
            var args = new List<ExpressionVariant>();
            foreach (var expr in Parameters)
                args.Add(expr.Evaluate(ref context));
            if (!context.ForeignFunctionInterface.Call(Name, args, out var res))
                return default;
            return res;
        }

        public override void CollectReferences(HashSet<(string parameter, string property)> references)
        {
            foreach(var arg in Parameters)
                arg.CollectReferences(references);
        }

        protected override string Print()
        {
            return Name + "( (" + string.Join("), (", Parameters) + ") )";
        }
    }
    
    internal class MemberAccessExpression : Expression
    {
        public override ExpressionType Type => ExpressionType.MemberAccess;
        public Expression Target { get; }
        public string Member { get; }

        public MemberAccessExpression(Expression target, string member)
        {
            Target = target;
            Member = string.Intern(member);
        }

        public override void CollectReferences(HashSet<(string parameter, string property)> references)
        {
            Target.CollectReferences(references);
            if (Target is ParameterExpression pe)
                references.Add((pe.Name, Member));
        }

        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
        {
            if (Target is KeywordExpression ke
                && ke.Keyword == ExpressionKeyword.Target)
            {
                return context.Target.GetProperty(Member);
            }

            if (Target is ParameterExpression pe)
            {
                var obj = context.Parameters?.GetObjectParameter(pe.Name);
                if (obj != null)
                {
                    return obj.GetProperty(Member);
                }
            }
            // Those are considered immutable
            return Target.Evaluate(ref context).GetProperty(Member);
        }

        protected override string Print()
        {
            return "(" + Target.ToString() + ")." + Member;
        }
    }

    internal class ParameterExpression : Expression
    {
        public string Name { get; }
        public override ExpressionType Type => ExpressionType.Parameter;

        public ParameterExpression(string name)
        {
            Name = name;
        }
        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
        {
            return context.Parameters?.GetParameter(Name) ?? default;
        }

        protected override string Print()
        {
            return "{" + Name + "}";
        }
    }

    internal class KeywordExpression : Expression
    {
        public override ExpressionType Type => ExpressionType.Keyword;
        public ExpressionKeyword Keyword { get; }

        public KeywordExpression(ExpressionKeyword keyword)
        {
            Keyword = keyword;
        }

        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
        {
            if (Keyword == ExpressionKeyword.StartingValue)
                return context.StartingValue;
            if (Keyword == ExpressionKeyword.CurrentValue)
                return context.CurrentValue;
            if (Keyword == ExpressionKeyword.FinalValue)
                return context.FinalValue;
            if (Keyword == ExpressionKeyword.Target)
                // should be handled by MemberAccess
                return default;
            if (Keyword == ExpressionKeyword.True)
                return true;
            if (Keyword == ExpressionKeyword.False)
                return false;
            if (Keyword == ExpressionKeyword.Pi)
                return (float) Math.PI;
            return default;
        }

        protected override string Print()
        {
            return "[" + Keyword + "]";
        }
    }

    internal class UnaryExpression : Expression
    {
        public Expression Parameter { get; }
        public override ExpressionType Type { get; }
        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
        {
            if (Type == ExpressionType.Not)
                return !Parameter.Evaluate(ref context);
            if (Type == ExpressionType.UnaryMinus)
                return -Parameter.Evaluate(ref context);
            return default;
        }

        public override void CollectReferences(HashSet<(string parameter, string property)> references)
        {
            Parameter.CollectReferences(references);
        }

        protected override string Print()
        {
            return OperatorName(Type) + Parameter;
        }

        public UnaryExpression(Expression parameter, ExpressionType type)
        {
            Parameter = parameter;
            Type = type;
        }
    }
    
    internal class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public override ExpressionType Type { get; }
        public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
        {
            var left = Left.Evaluate(ref context);
            var right = Right.Evaluate(ref context);
            if (Type == ExpressionType.Add)
                return left + right;
            if (Type == ExpressionType.Subtract)
                return left - right;
            if (Type == ExpressionType.Multiply)
                return left * right;
            if (Type == ExpressionType.Divide)
                return left / right;
            if (Type == ExpressionType.Remainder)
                return left % right;
            if (Type == ExpressionType.MoreThan)
                return left > right;
            if (Type == ExpressionType.LessThan)
                return left < right;
            if (Type == ExpressionType.MoreThanOrEqual)
                return left > right;
            if (Type == ExpressionType.LessThanOrEqual)
                return left < right;
            if (Type == ExpressionType.LogicalAnd)
                return left.And(right);
            if (Type == ExpressionType.LogicalOr)
                return left.Or(right);
            if (Type == ExpressionType.Equals)
                return left.EqualsTo(right);
            if (Type == ExpressionType.NotEquals)
                return left.NotEqualsTo(right);
            return default;
        }

        public override void CollectReferences(HashSet<(string parameter, string property)> references)
        {
            Left.CollectReferences(references);
            Right.CollectReferences(references);
        }

        protected override string Print()
        {
            return "(" + Left + OperatorName(Type) + Right + ")";
        }

        public BinaryExpression(Expression left, Expression right, ExpressionType type)
        {
            Left = left;
            Right = right;
            Type = type;
        }
    }



}
