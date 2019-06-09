using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Animation.Animators;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Animation
{

    internal class StringIndexerNode : IndexerNodeBase
    {
        public StringIndexerNode(IList<string> arguments)
        {
            Arguments = arguments;
        }

        public override string Description => "[" + string.Join(",", Arguments) + "]";

        protected override bool SetTargetValueCore(object value, BindingPriority priority)
        {
            var typeInfo = Target.Target.GetType().GetTypeInfo();
            var list = Target.Target as IList;
            var dictionary = Target.Target as IDictionary;
            var indexerProperty = GetIndexer(typeInfo);
            var indexerParameters = indexerProperty?.GetIndexParameters();

            if (indexerProperty != null && indexerParameters.Length == Arguments.Count)
            {
                var convertedObjectArray = new object[indexerParameters.Length];

                for (int i = 0; i < Arguments.Count; i++)
                {
                    object temp = null;

                    if (!TypeUtilities.TryConvert(indexerParameters[i].ParameterType, Arguments[i], CultureInfo.InvariantCulture, out temp))
                    {
                        return false;
                    }

                    convertedObjectArray[i] = temp;
                }

                var intArgs = convertedObjectArray.OfType<int>().ToArray();

                // Try special cases where we can validate indices
                if (typeInfo.IsArray)
                {
                    return SetValueInArray((Array)Target.Target, intArgs, value);
                }
                else if (Arguments.Count == 1)
                {
                    if (list != null)
                    {
                        if (intArgs.Length == Arguments.Count && intArgs[0] >= 0 && intArgs[0] < list.Count)
                        {
                            list[intArgs[0]] = value;
                            return true;
                        }

                        return false;
                    }
                    else if (dictionary != null)
                    {
                        if (dictionary.Contains(convertedObjectArray[0]))
                        {
                            dictionary[convertedObjectArray[0]] = value;
                            return true;
                        }
                        else
                        {
                            dictionary.Add(convertedObjectArray[0], value);
                            return true;
                        }
                    }
                    else
                    {
                        // Fallback to unchecked access
                        indexerProperty.SetValue(Target.Target, value, convertedObjectArray);
                        return true;
                    }
                }
                else
                {
                    // Fallback to unchecked access
                    indexerProperty.SetValue(Target.Target, value, convertedObjectArray);
                    return true;
                }
            }
            // Multidimensional arrays end up here because the indexer search picks up the IList indexer instead of the
            // multidimensional indexer, which doesn't take the same number of arguments
            else if (typeInfo.IsArray)
            {
                SetValueInArray((Array)Target.Target, value);
                return true;
            }
            return false;
        }

        private bool SetValueInArray(Array array, object value)
        {
            int[] intArgs;
            if (!ConvertArgumentsToInts(out intArgs))
                return false;
            return SetValueInArray(array, intArgs);
        }


        private bool SetValueInArray(Array array, int[] indices, object value)
        {
            if (ValidBounds(indices, array))
            {
                array.SetValue(value, indices);
                return true;
            }
            return false;
        }


        public IList<string> Arguments { get; }

        public override Type PropertyType => GetIndexer(Target.Target.GetType().GetTypeInfo())?.PropertyType;

        protected override object GetValue(object target)
        {
            var typeInfo = target.GetType().GetTypeInfo();
            var list = target as IList;
            var dictionary = target as IDictionary;
            var indexerProperty = GetIndexer(typeInfo);
            var indexerParameters = indexerProperty?.GetIndexParameters();

            if (indexerProperty != null && indexerParameters.Length == Arguments.Count)
            {
                var convertedObjectArray = new object[indexerParameters.Length];

                for (int i = 0; i < Arguments.Count; i++)
                {
                    object temp = null;

                    if (!TypeUtilities.TryConvert(indexerParameters[i].ParameterType, Arguments[i], CultureInfo.InvariantCulture, out temp))
                    {
                        return AvaloniaProperty.UnsetValue;
                    }

                    convertedObjectArray[i] = temp;
                }

                var intArgs = convertedObjectArray.OfType<int>().ToArray();

                // Try special cases where we can validate indices
                if (typeInfo.IsArray)
                {
                    return GetValueFromArray((Array)target, intArgs);
                }
                else if (Arguments.Count == 1)
                {
                    if (list != null)
                    {
                        if (intArgs.Length == Arguments.Count && intArgs[0] >= 0 && intArgs[0] < list.Count)
                        {
                            return list[intArgs[0]];
                        }

                        return AvaloniaProperty.UnsetValue;
                    }
                    else if (dictionary != null)
                    {
                        if (dictionary.Contains(convertedObjectArray[0]))
                        {
                            return dictionary[convertedObjectArray[0]];
                        }

                        return AvaloniaProperty.UnsetValue;
                    }
                    else
                    {
                        // Fallback to unchecked access
                        return indexerProperty.GetValue(target, convertedObjectArray);
                    }
                }
                else
                {
                    // Fallback to unchecked access
                    return indexerProperty.GetValue(target, convertedObjectArray);
                }
            }
            // Multidimensional arrays end up here because the indexer search picks up the IList indexer instead of the
            // multidimensional indexer, which doesn't take the same number of arguments
            else if (typeInfo.IsArray)
            {
                return GetValueFromArray((Array)target);
            }

            return AvaloniaProperty.UnsetValue;
        }

        private object GetValueFromArray(Array array)
        {
            int[] intArgs;
            if (!ConvertArgumentsToInts(out intArgs))
                return AvaloniaProperty.UnsetValue;
            return GetValueFromArray(array, intArgs);
        }

        private object GetValueFromArray(Array array, int[] indices)
        {
            if (ValidBounds(indices, array))
            {
                return array.GetValue(indices);
            }
            return AvaloniaProperty.UnsetValue;
        }

        private bool ConvertArgumentsToInts(out int[] intArgs)
        {
            intArgs = new int[Arguments.Count];

            for (int i = 0; i < Arguments.Count; ++i)
            {
                object value;

                if (!TypeUtilities.TryConvert(typeof(int), Arguments[i], CultureInfo.InvariantCulture, out value))
                {
                    return false;
                }

                intArgs[i] = (int)value;
            }
            return true;
        }

        private static PropertyInfo GetIndexer(TypeInfo typeInfo)
        {
            PropertyInfo indexer;

            for (; typeInfo != null; typeInfo = typeInfo.BaseType?.GetTypeInfo())
            {
                // Check for the default indexer name first to make this faster.
                // This will only be false when a class in VB has a custom indexer name.
                if ((indexer = typeInfo.GetDeclaredProperty(CommonPropertyNames.IndexerName)) != null)
                {
                    return indexer;
                }

                foreach (var property in typeInfo.DeclaredProperties)
                {
                    if (property.GetIndexParameters().Any())
                    {
                        return property;
                    }
                }
            }

            return null;
        }

        private bool ValidBounds(int[] indices, Array array)
        {
            if (indices.Length == array.Rank)
            {
                for (var i = 0; i < indices.Length; ++i)
                {
                    if (indices[i] >= array.GetLength(i))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        protected override bool ShouldUpdate(object sender, PropertyChangedEventArgs e)
        {
            var typeInfo = sender.GetType().GetTypeInfo();
            return typeInfo.GetDeclaredProperty(e.PropertyName)?.GetIndexParameters().Any() ?? false;
        }

        protected override int? TryGetFirstArgumentAsInt()
        {
            if (TypeUtilities.TryConvert(typeof(int), Arguments[0], CultureInfo.InvariantCulture, out var value))
            {
                return (int?)value;
            }
            return null;
        }
    }
    internal enum SourceMode
    {
        Data,
        Control
    }
    internal static class ArgumentListParser
    {
        public static IList<string> ParseArguments(this ref CharacterReader r, char open, char close, char delimiter = ',')
        {
            if (r.Peek == open)
            {
                var result = new List<string>();

                r.Take();

                while (!r.End)
                {
                    var argument = r.TakeWhile(c => c != delimiter && c != close && !char.IsWhiteSpace(c));
                    if (argument.IsEmpty)
                    {
                        throw new ExpressionParseException(r.Position, "Expected indexer argument.");
                    }

                    result.Add(argument.ToString());

                    r.SkipWhitespace();

                    if (r.End)
                    {
                        throw new ExpressionParseException(r.Position, $"Expected '{delimiter}'.");
                    }
                    else if (r.TakeIf(close))
                    {
                        return result;
                    }
                    else
                    {
                        if (r.Take() != delimiter)
                        {
                            throw new ExpressionParseException(r.Position, $"Expected '{delimiter}'.");
                        }

                        r.SkipWhitespace();
                    }
                }

                throw new ExpressionParseException(r.Position, $"Expected '{close}'.");
            }

            throw new ExpressionParseException(r.Position, $"Expected '{open}'.");
        }
    }

    internal class TargetExpressionParser
    {
        private readonly Func<string, string, Type> _typeResolver;
        private const bool _enableValidation = true;
        public TargetExpressionParser(Func<string, string, Type> typeResolver)
        {
            _typeResolver = typeResolver;
        }

        public ExpressionNode Parse(ref CharacterReader r)
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

        private State ParseStart(ref CharacterReader r, IList<ExpressionNode> nodes)
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

        private static bool ParseDot(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('.');
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


    /// <summary>
    /// Defines a KeyFrame that is used for
    /// <see cref="Animator{T}"/> objects.
    /// </summary>
    public class AnimatorKeyFrame : AvaloniaObject
    {
        public static readonly DirectProperty<AnimatorKeyFrame, object> ValueProperty =
            AvaloniaProperty.RegisterDirect<AnimatorKeyFrame, object>(nameof(Value), k => k.Value, (k, v) => k.Value = v);
 
        internal bool isNeutral;
        public Type AnimatorType { get; internal set; }
        public string TargetProperty { get; internal set; }
        public Cue Cue { get; internal set; }
        public AvaloniaProperty Property { get; private set; }

        private object _value;

        public object Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }

        public IDisposable BindSetter(IAnimationSetter setter, Animatable targetControl)
        {
            // Property = setter.Property;
            var value = setter.Value;

            if (value is IBinding binding)
            {
                return this.Bind(ValueProperty, binding, targetControl);
            }
            else
            {
                return this.Bind(ValueProperty, ObservableEx.SingleValue(value).ToBinding(), targetControl);
            }
        }

        public T GetTypedValue<T>()
        {
            var typeConv = TypeDescriptor.GetConverter(typeof(T));

            if (Value == null)
            {
                throw new ArgumentNullException($"KeyFrame value can't be null.");
            }
            if (Value is T typedValue)
            {
                return typedValue;
            }
            if (!typeConv.CanConvertTo(Value.GetType()))
            {
                throw new InvalidCastException($"KeyFrame value doesnt match property type.");
            }

            return (T)typeConv.ConvertTo(Value, typeof(T));
        }
    }
}
