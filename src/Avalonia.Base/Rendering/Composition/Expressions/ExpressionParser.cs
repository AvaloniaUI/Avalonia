using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

// ReSharper disable StringLiteralTypo

namespace Avalonia.Rendering.Composition.Expressions
{
    internal class ExpressionParser
    {
        public static Expression Parse(ReadOnlySpan<char> s)
        {
            var p = new TokenParser(s);
            var parsed = ParseTillTerminator(ref p, "", false, false, out _);
            p.SkipWhitespace();
            if (p.Length != 0)
                throw new ExpressionParseException("Unexpected data ", p.Position);
            return parsed;
        }

        private static ReadOnlySpan<char> Dot => ".".AsSpan();
        static bool TryParseAtomic(ref TokenParser parser, 
            [MaybeNullWhen(returnValue: false)] out Expression expr)
        {
            // We can parse keywords, parameter names and constants
            expr = null;
            if (parser.TryParseKeywordLowerCase("this.startingvalue"))
                expr = new KeywordExpression(ExpressionKeyword.StartingValue);
            else if(parser.TryParseKeywordLowerCase("this.currentvalue"))
                expr = new KeywordExpression(ExpressionKeyword.CurrentValue);
            else if(parser.TryParseKeywordLowerCase("this.finalvalue"))
                expr = new KeywordExpression(ExpressionKeyword.FinalValue);
            else if(parser.TryParseKeywordLowerCase("pi"))
                expr = new KeywordExpression(ExpressionKeyword.Pi);
            else if(parser.TryParseKeywordLowerCase("true"))
                expr = new KeywordExpression(ExpressionKeyword.True);
            else if(parser.TryParseKeywordLowerCase("false"))
                expr = new KeywordExpression(ExpressionKeyword.False);
            else if (parser.TryParseKeywordLowerCase("this.target"))
                expr = new KeywordExpression(ExpressionKeyword.Target);

            if (expr != null)
                return true;

            if (parser.TryParseIdentifier(out var identifier))
            {
                expr = new ParameterExpression(identifier.ToString());
                return true;
            }

            if(parser.TryParseFloat(out var scalar))
            {
                expr = new ConstantExpression(scalar);
                return true;
            }

            return false;

        }

        static bool TryParseOperator(ref TokenParser parser, out ExpressionType op)
        {
            op = (ExpressionType) (-1);
            if (parser.TryConsume("||"))
                op = ExpressionType.LogicalOr;
            else if (parser.TryConsume("&&"))
                op = ExpressionType.LogicalAnd;
            else if (parser.TryConsume(">="))
                op = ExpressionType.MoreThanOrEqual;
            else if (parser.TryConsume("<="))
                op = ExpressionType.LessThanOrEqual;
            else if (parser.TryConsume("=="))
                op = ExpressionType.Equals;
            else if (parser.TryConsume("!="))
                op = ExpressionType.NotEquals;
            else if (parser.TryConsumeAny("+-/*><%".AsSpan(), out var sop))
            {
#pragma warning disable CS8509
                op = sop switch
#pragma warning restore CS8509
                {
                    '+' => ExpressionType.Add,
                    '-' => ExpressionType.Subtract,
                    '/' => ExpressionType.Divide,
                    '*' => ExpressionType.Multiply,
                    '<' => ExpressionType.LessThan,
                    '>' => ExpressionType.MoreThan,
                    '%' => ExpressionType.Remainder
                };
            }
            else
                return false;

            return true;
        }


        struct ExpressionOperatorGroup
        {
            private List<Expression> _expressions;
            private List<ExpressionType> _operators;
            private Expression? _first;

            public bool NotEmpty => !Empty;
            public bool Empty => _expressions == null && _first == null;

            public void AppendFirst(Expression expr)
            {
                if (NotEmpty)
                    throw new InvalidOperationException();
                _first = expr;
            }

            public void AppendWithOperator(Expression expr, ExpressionType op)
            {
                if (_expressions == null)
                {
                    if (_first == null)
                        throw new InvalidOperationException();
                    _expressions = new List<Expression>();
                    _expressions.Add(_first);
                    _first = null;
                    _operators = new List<ExpressionType>();
                }
                _expressions.Add(expr);
                _operators.Add(op);
            }

            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/
            private static readonly ExpressionType[][] OperatorPrecedenceGroups = new[]
            {
                // multiplicative
                new[] {ExpressionType.Multiply, ExpressionType.Divide, ExpressionType.Remainder},
                // additive
                new[] {ExpressionType.Add, ExpressionType.Subtract},
                // relational
                new[] {ExpressionType.MoreThan, ExpressionType.MoreThanOrEqual, ExpressionType.LessThan, ExpressionType.LessThanOrEqual},
                // equality
                new[] {ExpressionType.Equals, ExpressionType.NotEquals},
                // conditional AND
                new[] {ExpressionType.LogicalAnd},
                // conditional OR
                new[]{ ExpressionType.LogicalOr},
            };

            private static readonly ExpressionType[][] OperatorPrecedenceGroupsReversed =
                OperatorPrecedenceGroups.Reverse().ToArray();

            // a*b+c [a,b,c] [*,+], call with (0, 2)
            // ToExpression(a*b) + ToExpression(c)
            // a+b*c -> ToExpression(a) + ToExpression(b*c)
            Expression ToExpression(int from, int to)
            {
                if (to - from == 0)
                    return _expressions[from];
                
                if (to - from == 1)
                    return new BinaryExpression(_expressions[from], _expressions[to], _operators[from]);
                
                foreach (var grp in OperatorPrecedenceGroupsReversed)
                {
                    for (var c = from; c < to; c++)
                    {
                        var currentOperator = _operators[c];
                        foreach(var operatorFromGroup in grp)
                            if (currentOperator == operatorFromGroup)
                            {
                                // We are dividing the expression right here
                                var left = ToExpression(from, c);
                                var right = ToExpression(c + 1, to);
                                return new BinaryExpression(left, right, currentOperator);
                            }
                    }
                }

                // We shouldn't ever get here, if we are, there is something wrong in the code
                throw new ExpressionParseException("Expression parsing algorithm bug in ToExpression", 0);
            }

            public Expression ToExpression()
            {
                if (_expressions == null)
                    return _first ?? throw new InvalidOperationException();
                return ToExpression(0, _expressions.Count - 1);
            }
        }

        static Expression ParseTillTerminator(ref TokenParser parser, string terminatorChars, 
            bool throwOnTerminator,
            bool throwOnEnd,
            out char? token)
        {
            ExpressionOperatorGroup left = default;
            token = null;
            while (true)
            {
                if (parser.TryConsumeAny(terminatorChars.AsSpan(), out var consumedToken))
                {
                    if (throwOnTerminator || left.Empty)
                        throw new ExpressionParseException($"Unexpected '{token}'", parser.Position - 1);
                    token = consumedToken;
                    return left.ToExpression();
                }
                parser.SkipWhitespace();
                if (parser.Length == 0)
                {
                    if (throwOnEnd || left.Empty)
                        throw new ExpressionParseException("Unexpected end of  expression", parser.Position);
                    return left.ToExpression();
                }
                
                ExpressionType? op = null;
                if (left.NotEmpty)
                {
                    if (parser.TryConsume('?'))
                    {
                        var truePart = ParseTillTerminator(ref parser, ":",
                            false, true, out _);
                        // pass through the current parsing rules to consume the rest
                        var falsePart = ParseTillTerminator(ref parser, terminatorChars, throwOnTerminator, throwOnEnd,
                            out token);
                        
                        return new ConditionalExpression(left.ToExpression(), truePart, falsePart);
                    }
                    
                    // We expect a binary operator here
                    if (!TryParseOperator(ref parser, out var sop))
                        throw new ExpressionParseException("Unexpected token", parser.Position);
                    op = sop;
                }
                
                // We expect an expression to be parsed (either due to expecting a binary operator or parsing the first part
                var applyNegation = false;
                while (parser.TryConsume('!')) 
                    applyNegation = !applyNegation;

                var applyUnaryMinus = false;
                while (parser.TryConsume('-')) 
                    applyUnaryMinus = !applyUnaryMinus;

                Expression? parsed;
                
                if (parser.TryConsume('(')) 
                    parsed = ParseTillTerminator(ref parser, ")", false, true, out _);
                else if (parser.TryParseCall(out var functionName))
                {
                    var parameterList = new List<Expression>();
                    while (true)
                    {
                        parameterList.Add(ParseTillTerminator(ref parser, ",)", false, true, out var closingToken));
                        if (closingToken == ')')
                            break;
                        if (closingToken != ',')
                            throw new ExpressionParseException("Unexpected end of the expression", parser.Position);
                    }

                    parsed = new FunctionCallExpression(functionName.ToString(), parameterList);
                }
                else if (TryParseAtomic(ref parser, out parsed))
                {
                    // do nothing
                }
                else
                    throw new ExpressionParseException("Unexpected token", parser.Position);

                
                // Parse any following member accesses
                while (parser.TryConsume('.'))
                {
                    if(!parser.TryParseIdentifier(out var memberName))
                        throw new ExpressionParseException("Unexpected token", parser.Position);

                    parsed = new MemberAccessExpression(parsed, memberName.ToString());
                }

                // Apply ! operator
                if (applyNegation)
                    parsed = new UnaryExpression(parsed, ExpressionType.Not);

                if (applyUnaryMinus)
                {
                    if(parsed is ConstantExpression constexpr)
                        parsed = new ConstantExpression(-constexpr.Constant);
                    else parsed = new UnaryExpression(parsed, ExpressionType.UnaryMinus);
                }

                if (left.Empty)
                    left.AppendFirst(parsed);
                else
                    left.AppendWithOperator(parsed, op!.Value);
            }
            
            
            
        }
    }
}
