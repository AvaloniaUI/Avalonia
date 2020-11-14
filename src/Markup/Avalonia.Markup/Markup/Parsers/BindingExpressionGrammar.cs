﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;

namespace Avalonia.Markup.Parsers
{
    internal enum SourceMode
    {
        Data,
        Control
    }

    internal static class BindingExpressionGrammar
    {
        public static (List<INode> Nodes, SourceMode Mode) Parse(ref CharacterReader r)
        {
            var nodes = new List<INode>();
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

            return (nodes, mode);
        }

        private static State ParseStart(ref CharacterReader r, IList<INode> nodes)
        {
            if (ParseNot(ref r))
            {
                nodes.Add(new NotNode());
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
                    nodes.Add(new PropertyNameNode { PropertyName = identifier.ToString() });
                    return State.AfterMember;
                }
            }

            return State.End;
        }

        private static State ParseAfterMember(ref CharacterReader r, IList<INode> nodes)
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

        private static State ParseBeforeMember(ref CharacterReader r, IList<INode> nodes)
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
                    nodes.Add(new PropertyNameNode { PropertyName = identifier.ToString() });
                    return State.AfterMember;
                }

                return State.End;
            }
        }

        private static State ParseAttachedProperty(ref CharacterReader r, List<INode> nodes)
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

            nodes.Add(new AttachedPropertyNameNode
            {
                Namespace = ns.ToString(),
                TypeName = owner.ToString(),
                PropertyName = name.ToString()
            });
            return State.AfterMember;
        }

        private static State ParseIndexer(ref CharacterReader r, List<INode> nodes)
        {
            var args = r.ParseArguments('[', ']');

            if (args.Count == 0)
            {
                throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
            }

            nodes.Add(new IndexerNode { Arguments = args });
            return State.AfterMember;
        }

        private static State ParseElementName(ref CharacterReader r, List<INode> nodes)
        {
            var name = r.ParseIdentifier();

            if (name.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Element name expected after '#'.");
            }

            nodes.Add(new NameNode { Name = name.ToString() });
            return State.AfterMember;
        }

        private static State ParseRelativeSource(ref CharacterReader r, List<INode> nodes)
        {
            var mode = r.ParseIdentifier();

            if (mode.SequenceEqual("self".AsSpan()))
            {
                nodes.Add(new SelfNode());
            }
            else if (mode.SequenceEqual("parent".AsSpan()))
            {
                string ancestorNamespace = null;
                string ancestorType = null;
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
                            (ancestorNamespace, ancestorType) = ParseTypeName(ref reader);
                        }
                    }
                    else
                    {
                        var reader = new CharacterReader(args[0].AsSpan());
                        (ancestorNamespace, ancestorType) = ParseTypeName(ref reader);
                        ancestorLevel = int.Parse(args[1]);
                    }
                }
                nodes.Add(new AncestorNode
                {
                    Namespace = ancestorNamespace,
                    TypeName = ancestorType,
                    Level = ancestorLevel
                });
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

            public void Deconstruct(out string ns, out string typeName)
            {
                ns = Namespace.ToString();
                typeName = Type.ToString();
            }
        }

        public interface INode {}

        public interface ITransformNode {}

        public class EmptyExpressionNode : INode { }

        public class PropertyNameNode : INode
        {
            public string PropertyName { get; set; }
        }

        public class AttachedPropertyNameNode : INode
        {
            public string Namespace { get; set; }
            public string TypeName { get; set; }
            public string PropertyName { get; set; }
        }

        public class IndexerNode : INode
        {
            public IList<string> Arguments { get; set; }
        }

        public class NotNode : INode, ITransformNode {}

        public class StreamNode : INode {}

        public class SelfNode : INode {}

        public class NameNode : INode
        {
            public string Name { get; set; }
        }

        public class AncestorNode : INode
        {
            public string Namespace { get; set; }
            public string TypeName { get; set; }
            public int Level { get; set; }
        }
    }
}
