// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Data.Parsers
{
    internal class ExpressionParser
    {
        private bool _enableValidation;

        public ExpressionParser(bool enableValidation)
        {
            _enableValidation = enableValidation;
        }

        public ExpressionNode Parse(Reader r)
        {
            var nodes = new List<ExpressionNode>();
            var state = State.Start;

            while (!r.End && state != State.End)
            {
                switch (state)
                {
                    case State.Start:
                        state = ParseStart(r, nodes);
                        break;

                    case State.AfterMember:
                        state = ParseAfterMember(r, nodes);
                        break;

                    case State.BeforeMember:
                        state = ParseBeforeMember(r, nodes);
                        break;

                    case State.AttachedProperty:
                        state = ParseAttachedProperty(r, nodes);
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

        private State ParseStart(Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseNot(r))
            {
                nodes.Add(new LogicalNotNode());
                return State.Start;
            }
            else if (ParseOpenBrace(r))
            {
                return State.AttachedProperty;
            }
            else
            {
                var identifier = IdentifierParser.Parse(r);

                if (identifier != null)
                {
                    nodes.Add(new PropertyAccessorNode(identifier, _enableValidation));
                    return State.AfterMember;
                }
            }

            return State.End;
        }

        private static State ParseAfterMember(Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseMemberAccessor(r))
            {
                return State.BeforeMember;
            }
            else if (ParseStreamOperator(r))
            {
                nodes.Add(new StreamNode());
                return State.AfterMember;
            }
            else
            {
                var args = ArgumentListParser.Parse(r, '[', ']');

                if (args != null)
                {
                    if (args.Count == 0)
                    {
                        throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
                    }

                    nodes.Add(new IndexerNode(args));
                    return State.AfterMember;
                }
            }

            return State.End;
        }

        private State ParseBeforeMember(Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseOpenBrace(r))
            {
                return State.AttachedProperty;
            }
            else
            {
                var identifier = IdentifierParser.Parse(r);

                if (identifier != null)
                {
                    nodes.Add(new PropertyAccessorNode(identifier, _enableValidation));
                    return State.AfterMember;
                }

                return State.End;
            }
        }

        private State ParseAttachedProperty(Reader r, List<ExpressionNode> nodes)
        {
            var owner = IdentifierParser.Parse(r);

            if (r.End || !r.TakeIf('.'))
            {
                throw new ExpressionParseException(r.Position, "Invalid attached property name.");
            }

            var name = IdentifierParser.Parse(r);

            if (r.End || !r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, "Expected ')'.");
            }

            nodes.Add(new PropertyAccessorNode(owner + '.' + name, _enableValidation));
            return State.AfterMember;
        }

        private static bool ParseNot(Reader r)
        {
            return !r.End && r.TakeIf('!');
        }

        private static bool ParseMemberAccessor(Reader r)
        {
            return !r.End && r.TakeIf('.');
        }

        private static bool ParseOpenBrace(Reader r)
        {
            return !r.End && r.TakeIf('(');
        }

        private static bool ParseStreamOperator(Reader r)
        {
            return !r.End && r.TakeIf('^');
        }

        private enum State
        {
            Start,
            AfterMember,
            BeforeMember,
            AttachedProperty,
            End,
        }
    }
}
