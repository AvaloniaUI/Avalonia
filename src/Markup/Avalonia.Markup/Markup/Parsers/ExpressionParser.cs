// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Avalonia.Markup.Parsers.Nodes;
using Avalonia.Utilities;
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

        public ExpressionNode Parse(CharacterReader r)
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

                    case State.Indexer:
                        state = ParseIndexer(r, nodes);
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

        private State ParseStart(CharacterReader r, IList<ExpressionNode> nodes)
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
            else if (PeekOpenBracket(r))
            {
                return State.Indexer;
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

        private static State ParseAfterMember(CharacterReader r, IList<ExpressionNode> nodes)
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
            else if (PeekOpenBracket(r))
            {
                return State.Indexer;
            }

            return State.End;
        }

        private State ParseBeforeMember(CharacterReader r, IList<ExpressionNode> nodes)
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

        private State ParseAttachedProperty(CharacterReader r, List<ExpressionNode> nodes)
        {
            string ns = string.Empty;
            string owner;
            var ownerOrNamespace = IdentifierParser.Parse(r);

            if (r.TakeIf(':'))
            {
                ns = ownerOrNamespace;
                owner = IdentifierParser.Parse(r);
            }
            else
            {
                owner = ownerOrNamespace;
            }

            if (r.End || !r.TakeIf('.'))
            {
                throw new ExpressionParseException(r.Position, "Invalid attached property name.");
            }

            var name = IdentifierParser.Parse(r);

            if (r.End || !r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, "Expected ')'.");
            }

            if (_typeResolver == null)
            {
                throw new InvalidOperationException("Cannot parse a binding path with an attached property without a type resolver. Maybe you can use a LINQ Expression binding path instead?");
            }

            var property = AvaloniaPropertyRegistry.Instance.FindRegistered(_typeResolver(ns, owner), name);

            nodes.Add(new AvaloniaPropertyAccessorNode(property, _enableValidation));
            return State.AfterMember;
        }

        private State ParseIndexer(CharacterReader r, List<ExpressionNode> nodes)
        {
            var args = ArgumentListParser.Parse(r, '[', ']');

            if (args.Count == 0)
            {
                throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
            }

            nodes.Add(new StringIndexerNode(args));
            return State.AfterMember;
        }
        
        private static bool ParseNot(CharacterReader r)
        {
            return !r.End && r.TakeIf('!');
        }

        private static bool ParseMemberAccessor(CharacterReader r)
        {
            return !r.End && r.TakeIf('.');
        }

        private static bool ParseOpenBrace(CharacterReader r)
        {
            return !r.End && r.TakeIf('(');
        }

        private static bool PeekOpenBracket(CharacterReader r)
        {
            return !r.End && r.Peek == '[';
        }

        private static bool ParseStreamOperator(CharacterReader r)
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
