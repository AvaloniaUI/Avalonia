// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Avalonia.Markup.Parsers.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Parsers
{
    internal class ExpressionParser
    {
        private readonly bool _enableValidation;
        private readonly Func<string, string, Type> _typeResolver;

        public ExpressionParser(bool enableValidation, Func<string, string, Type> typeResolver)
        {
            _typeResolver = typeResolver;
            _enableValidation = enableValidation;
        }

        public ExpressionNode Parse(ref Reader r)
        {
            var nodes = new List<ExpressionNode>();
            var state = State.Start;

            while (!r.End && state != State.End)
            {
                switch (state)
                {
                    case State.Start:
                        state = ParseStart(ref r, nodes);
                        break;

                    case State.AfterMember:
                        state = ParseAfterMember(ref r, nodes);
                        break;

                    case State.BeforeMember:
                        state = ParseBeforeMember(ref r, nodes);
                        break;

                    case State.AttachedProperty:
                        state = ParseAttachedProperty(ref r, nodes);
                        break;

                    case State.Indexer:
                        state = ParseIndexer(ref r, nodes);
                        break;
                }
            }

            if (state == State.BeforeMember)
            {
                throw new ExpressionParseException(r.Position, "Unexpected end of expression.");
            }

            for (int n = 0; n < nodes.Count - 1; ++n)
            {
                nodes[n].Next = nodes[n + 1];
            }

            return nodes.FirstOrDefault();
        }

        private State ParseStart(ref Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseNot(ref r))
            {
                nodes.Add(new LogicalNotNode());
                return State.Start;
            }
            else if (ParseOpenBrace(ref r))
            {
                return State.AttachedProperty;
            }
            else if (PeekOpenBracket(ref r))
            {
                return State.Indexer;
            }
            else
            {
                var identifier = r.ParseIdentifier();

                if (!identifier.IsEmpty)
                {
                    nodes.Add(new PropertyAccessorNode(identifier.ToString(), _enableValidation));
                    return State.AfterMember;
                }
            }

            return State.End;
        }

        private static State ParseAfterMember(ref Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseMemberAccessor(ref r))
            {
                return State.BeforeMember;
            }
            else if (ParseStreamOperator(ref r))
            {
                nodes.Add(new StreamNode());
                return State.AfterMember;
            }
            else if (PeekOpenBracket(ref r))
            {
                return State.Indexer;
            }

            return State.End;
        }

        private State ParseBeforeMember(ref Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseOpenBrace(ref r))
            {
                return State.AttachedProperty;
            }
            else
            {
                var identifier = r.ParseIdentifier();

                if (!identifier.IsEmpty)
                {
                    nodes.Add(new PropertyAccessorNode(identifier.ToString(), _enableValidation));
                    return State.AfterMember;
                }

                return State.End;
            }
        }

        private State ParseAttachedProperty(ref Reader r, List<ExpressionNode> nodes)
        {
            ReadOnlySpan<char> ns = ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> owner;
            var ownerOrNamespace = r.ParseIdentifier();

            if (r.TakeIf(':'))
            {
                ns = ownerOrNamespace;
                owner = r.ParseIdentifier();
            }
            else
            {
                owner = ownerOrNamespace;
            }

            if (r.End || !r.TakeIf('.'))
            {
                throw new ExpressionParseException(r.Position, "Invalid attached property name.");
            }

            var name = r.ParseIdentifier();

            if (r.End || !r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, "Expected ')'.");
            }

            var property = AvaloniaPropertyRegistry.Instance.FindRegistered(_typeResolver(ns.ToString(), owner.ToString()), name.ToString());

            nodes.Add(new AvaloniaPropertyAccessorNode(property, _enableValidation));
            return State.AfterMember;
        }

        private State ParseIndexer(ref Reader r, List<ExpressionNode> nodes)
        {
            var args = r.ParseArguments('[', ']');

            if (args.Count == 0)
            {
                throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
            }

            nodes.Add(new StringIndexerNode(args));
            return State.AfterMember;
        }
        
        private static bool ParseNot(ref Reader r)
        {
            return !r.End && r.TakeIf('!');
        }

        private static bool ParseMemberAccessor(ref Reader r)
        {
            return !r.End && r.TakeIf('.');
        }

        private static bool ParseOpenBrace(ref Reader r)
        {
            return !r.End && r.TakeIf('(');
        }

        private static bool PeekOpenBracket(ref Reader r)
        {
            return !r.End && r.Peek == '[';
        }

        private static bool ParseStreamOperator(ref Reader r)
        {
            return !r.End && r.TakeIf('^');
        }

        private enum State
        {
            Start,
            AfterMember,
            BeforeMember,
            AttachedProperty,
            Indexer,
            End,
        }
    }
}
