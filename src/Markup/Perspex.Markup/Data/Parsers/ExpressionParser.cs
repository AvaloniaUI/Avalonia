// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Perspex.Markup.Data.Parsers
{
    internal static class ExpressionParser
    {
        public static ExpressionNode Parse(Reader r)
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
                }
            }

            if (state == State.BeforeMember)
            {
                throw new ExpressionParseException(r, "Unexpected end of expression.");
            }

            for (int n = 0; n < nodes.Count - 1; ++n)
            {
                nodes[n].Next = nodes[n + 1];
            }

            return nodes.FirstOrDefault();
        }

        private static State ParseStart(Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseNot(r))
            {
                nodes.Add(new LogicalNotNode());
                return State.Start;
            }
            else
            {
                var identifier = IdentifierParser.Parse(r);

                if (identifier != null)
                {
                    nodes.Add(new PropertyAccessorNode(identifier));
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
            else
            {
                var args = ArgumentListParser.Parse(r, '[', ']');

                if (args != null)
                {
                    if (args.Count == 0)
                    {
                        throw new ExpressionParseException(r, "Indexer may not be empty.");
                    }

                    nodes.Add(new IndexerNode(args));
                    return State.AfterMember;
                }
            }

            return State.End;
        }

        private static State ParseBeforeMember(Reader r, IList<ExpressionNode> nodes)
        {
            var identifier = IdentifierParser.Parse(r);

            if (identifier != null)
            {
                nodes.Add(new PropertyAccessorNode(identifier));
                return State.AfterMember;
            }

            return State.End;
        }

        private static bool ParseNot(Reader r)
        {
            return !r.End && r.TakeIf('!');
        }

        private static bool ParseMemberAccessor(Reader r)
        {
            return !r.End && r.TakeIf('.');
        }

        private enum State
        {
            Start,
            AfterMember,
            BeforeMember,
            End,
        }
    }
}
