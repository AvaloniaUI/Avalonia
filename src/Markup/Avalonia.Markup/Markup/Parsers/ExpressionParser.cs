// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Avalonia.Markup.Parsers.Nodes;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

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
        private readonly INameScope _nameScope;

        public ExpressionParser(bool enableValidation, Func<string, string, Type> typeResolver, INameScope nameScope)
        {
            _typeResolver = typeResolver;
            _nameScope = nameScope;
            _enableValidation = enableValidation;
        }

        public (ExpressionNode Node, SourceMode Mode) Parse(ref CharacterReader r)
        {
            var nodes = new List<ExpressionNode>();
            var state = State.Start;
            var mode = SourceMode.Data;

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

                    case State.ElementName:
                        state = ParseElementName(ref r, nodes);
                        mode = SourceMode.Control;
                        break;

                    case State.RelativeSource:
                        state = ParseRelativeSource(ref r, nodes);
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

        private State ParseStart(ref CharacterReader r, IList<ExpressionNode> nodes)
        {
            if (ParseNot(ref r))
            {
                nodes.Add(new LogicalNotNode());
                return State.Start;
            }

            else if (ParseSharp(ref r))
            {
                return State.ElementName;
            }
            else if (ParseDollarSign(ref r))
            {
                return State.RelativeSource;
            }
            else if (ParseOpenBrace(ref r))
            {
                return State.AttachedProperty;
            }
            else if (PeekOpenBracket(ref r))
            {
                return State.Indexer;
            }
            else if (ParseDot(ref r))
            {
                nodes.Add(new EmptyExpressionNode());
                return State.End;
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

        private static State ParseAfterMember(ref CharacterReader r, IList<ExpressionNode> nodes)
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

        private State ParseBeforeMember(ref CharacterReader r, IList<ExpressionNode> nodes)
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

        private State ParseAttachedProperty(ref CharacterReader r, List<ExpressionNode> nodes)
        {
            var (ns, owner) = ParseTypeName(ref r);

            if (r.End || !r.TakeIf('.'))
            {
                throw new ExpressionParseException(r.Position, "Invalid attached property name.");
            }

            var name = r.ParseIdentifier();

            if (r.End || !r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, "Expected ')'.");
            }

            if (_typeResolver == null)
            {
                throw new InvalidOperationException("Cannot parse a binding path with an attached property without a type resolver. Maybe you can use a LINQ Expression binding path instead?");
            }

            var property = AvaloniaPropertyRegistry.Instance.FindRegistered(_typeResolver(ns.ToString(), owner.ToString()), name.ToString());

            nodes.Add(new AvaloniaPropertyAccessorNode(property, _enableValidation));
            return State.AfterMember;
        }

        private State ParseIndexer(ref CharacterReader r, List<ExpressionNode> nodes)
        {
            var args = r.ParseArguments('[', ']');

            if (args.Count == 0)
            {
                throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
            }

            nodes.Add(new StringIndexerNode(args));
            return State.AfterMember;
        }

        private State ParseElementName(ref CharacterReader r, List<ExpressionNode> nodes)
        {
            var name = r.ParseIdentifier();

            if (name == null)
            {
                throw new ExpressionParseException(r.Position, "Element name expected after '#'.");
            }

            nodes.Add(new ElementNameNode(_nameScope, name.ToString()));
            return State.AfterMember;
        }

        private State ParseRelativeSource(ref CharacterReader r, List<ExpressionNode> nodes)
        {
            var mode = r.ParseIdentifier();

            if (mode.Equals("self".AsSpan(), StringComparison.InvariantCulture))
            {
                nodes.Add(new SelfNode());
            }
            else if (mode.Equals("parent".AsSpan(), StringComparison.InvariantCulture))
            {
                Type ancestorType = null;
                var ancestorLevel = 0;
                if (PeekOpenBracket(ref r))
                {
                    var args = r.ParseArguments('[', ']', ';');
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
                            var reader = new CharacterReader(args[0].AsSpan());
                            var typeName = ParseTypeName(ref reader);
                            ancestorType = _typeResolver(typeName.Namespace.ToString(), typeName.Type.ToString());
                        }
                    }
                    else
                    {
                        var reader = new CharacterReader(args[0].AsSpan());
                        var typeName = ParseTypeName(ref reader);
                        ancestorType = _typeResolver(typeName.Namespace.ToString(), typeName.Type.ToString());
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
        
        private static TypeName ParseTypeName(ref CharacterReader r)
        {
            ReadOnlySpan<char> ns, typeName;
            ns = ReadOnlySpan<char>.Empty;
            var typeNameOrNamespace = r.ParseIdentifier();

            if (!r.End && r.TakeIf(':'))
            {
                ns = typeNameOrNamespace;
                typeName = r.ParseIdentifier();
            }
            else
            {
                typeName = typeNameOrNamespace;
            }

            return new TypeName(ns, typeName);
        }
      
        private static bool ParseNot(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('!');
        }

        private static bool ParseMemberAccessor(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('.');
        }

        private static bool ParseOpenBrace(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('(');
        }

        private static bool PeekOpenBracket(ref CharacterReader r)
        {
            return !r.End && r.Peek == '[';
        }

        private static bool ParseStreamOperator(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('^');
        }

        private static bool ParseDollarSign(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('$');
        }

        private static bool ParseSharp(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('#');
        }

        private static bool ParseDot(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('.');
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

        private readonly ref struct TypeName
        {
            public TypeName(ReadOnlySpan<char> ns, ReadOnlySpan<char> typeName)
            {
                Namespace = ns;
                Type = typeName;
            }

            public readonly ReadOnlySpan<char> Namespace;
            public readonly ReadOnlySpan<char> Type;

            public void Deconstruct(out ReadOnlySpan<char> ns, out ReadOnlySpan<char> typeName)
            {
                ns = Namespace;
                typeName = Type;
            }
        }
    }
}
