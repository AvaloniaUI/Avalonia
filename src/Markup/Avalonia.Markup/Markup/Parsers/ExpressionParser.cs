// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Avalonia.Markup.Parsers.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Parsers
{
    internal enum SourceMode
    {
        Data,
        Control
    }

    internal class ExpressionParser
    {
        private readonly bool _enableValidation;
        private readonly Func<string, string, Type> _typeResolver;

        public ExpressionParser(bool enableValidation, Func<string, string, Type> typeResolver)
        {
            _typeResolver = typeResolver;
            _enableValidation = enableValidation;
        }

        public (ExpressionNode Node, SourceMode Mode) Parse(Reader r)
        {
            var nodes = new List<ExpressionNode>();
            var state = State.Start;
            var mode = SourceMode.Data;

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

                    case State.ElementName:
                        state = ParseElementName(r, nodes);
                        mode = SourceMode.Control;
                        break;

                    case State.RelativeSource:
                        state = ParseRelativeSource(r, nodes);
                        mode = SourceMode.Control;
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

            return (nodes.FirstOrDefault(), mode);
        }

        private State ParseStart(Reader r, IList<ExpressionNode> nodes)
        {
            if (ParseNot(r))
            {
                nodes.Add(new LogicalNotNode());
                return State.Start;
            }
            else if (ParseSharp(r))
            {
                return State.ElementName;
            }
            else if (ParseDollarSign(r))
            {
                return State.RelativeSource;
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
            else if (PeekOpenBracket(r))
            {
                return State.Indexer;
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
            var (ns, owner) = ParseTypeName(r);

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

        private State ParseIndexer(Reader r, List<ExpressionNode> nodes)
        {
            var args = ArgumentListParser.Parse(r, '[', ']');

            if (args.Count == 0)
            {
                throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
            }

            nodes.Add(new StringIndexerNode(args));
            return State.AfterMember;
        }
        
        private State ParseElementName(Reader r, List<ExpressionNode> nodes)
        {
            var name = IdentifierParser.Parse(r);

            if (name == null)
            {
                throw new ExpressionParseException(r.Position, "Element name expected after '#'.");
            }

            nodes.Add(new ElementNameNode(name));
            return State.AfterMember;
        }


        private State ParseRelativeSource(Reader r, List<ExpressionNode> nodes)
        {
            var mode = IdentifierParser.Parse(r);

            if (mode == "self")
            {
                nodes.Add(new SelfNode());
            }
            else if (mode == "parent")
            {
                Type ancestorType = null;
                var ancestorLevel = 0;
                if (PeekOpenBracket(r))
                {
                    var args = ArgumentListParser.Parse(r, '[', ']', ';');
                    if (args.Count > 2 || args.Count == 0)
                    {
                        throw new ExpressionParseException(r.Position, "Too many arguments in RelativeSource syntax sugar");
                    }
                    else if (args.Count == 1)
                    {
                        if (int.TryParse(args[0], out int level))
                        {
                            ancestorType = null;
                            ancestorLevel = level;
                        }
                        else
                        {
                            var typeName = ParseTypeName(new Reader(args[0]));
                            ancestorType = _typeResolver(typeName.ns, typeName.typeName);
                        }
                    }
                    else
                    {
                        var typeName = ParseTypeName(new Reader(args[0]));
                        ancestorType = _typeResolver(typeName.ns, typeName.typeName);
                        ancestorLevel = int.Parse(args[1]);
                    }
                }
                nodes.Add(new FindAncestorNode(ancestorType, ancestorLevel));
            }
            else
            {
                throw new ExpressionParseException(r.Position, "Unknown RelativeSource mode.");
            }

            return State.AfterMember;
        }
        
        private static (string ns, string typeName) ParseTypeName(Reader r)
        {
            string ns, typeName;
            ns = string.Empty;
            var typeNameOrNamespace = IdentifierParser.Parse(r);

            if (!r.End && r.TakeIf(':'))
            {
                ns = typeNameOrNamespace;
                typeName = IdentifierParser.Parse(r);
            }
            else
            {
                typeName = typeNameOrNamespace;
            }

            return (ns, typeName);
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

        private static bool PeekOpenBracket(Reader r)
        {
            return !r.End && r.Peek == '[';
        }

        private static bool ParseStreamOperator(Reader r)
        {
            return !r.End && r.TakeIf('^');
        }

        private static bool ParseDollarSign(Reader r)
        {
            return !r.End && r.TakeIf('$');
        }

        private static bool ParseSharp(Reader r)
        {
            return !r.End && r.TakeIf('#');
        }

        private enum State
        {
            Start,
            RelativeSource,
            ElementName,
            AfterMember,
            BeforeMember,
            AttachedProperty,
            Indexer,
            End,
        }
    }
}
