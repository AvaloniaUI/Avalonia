using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Styling
{
    /// <summary>
    /// Extension methods for <see cref="Selector"/>.
    /// </summary>
    public static class Selectors
    {
        /// <summary>
        /// Returns a selector which matches a previous selector's child.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Child(this Selector previous)
        {
            return new ChildSelector(previous);
        }

        /// <summary>
        /// Returns a selector which matches a control's style class.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="name">The name of the style class.</param>
        /// <returns>The selector.</returns>
        public static Selector Class(this Selector? previous, string name)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name may not be empty", nameof(name));
            }

            var tac = previous as TypeNameAndClassSelector;

            if (tac != null)
            {
                tac.Classes.Add(name);
                return tac;
            }
            else
            {
                return TypeNameAndClassSelector.ForClass(previous, name);
            }
        }

        /// <summary>
        /// Returns a selector which matches a descendant of a previous selector.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Descendant(this Selector? previous)
        {
            return new DescendantSelector(previous);
        }

        /// <summary>
        /// Returns a selector which matches a type or a derived type.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="type">The type.</param>
        /// <returns>The selector.</returns>
        public static Selector Is(this Selector? previous, Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

            return TypeNameAndClassSelector.Is(previous, type);
        }

        /// <summary>
        /// Returns a selector which matches a type or a derived type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Is<T>(this Selector? previous) where T : StyledElement
        {
            return previous.Is(typeof(T));
        }

        /// <summary>
        /// Returns a selector which matches a control's Name.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="name">The name.</param>
        /// <returns>The selector.</returns>
        public static Selector Name(this Selector? previous, string name)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name may not be empty", nameof(name));
            }

            var tac = previous as TypeNameAndClassSelector;

            if (tac != null)
            {
                tac.Name = name;
                return tac;
            }
            else
            {
                return TypeNameAndClassSelector.ForName(previous, name);
            }
        }

        public static Selector Nesting(this Selector? previous)
        {
            return new NestingSelector();
        }

        /// <summary>
        /// Returns a selector which inverts the results of selector argument.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="argument">The selector to be not-ed.</param>
        /// <returns>The selector.</returns>
        public static Selector Not(this Selector? previous, Func<Selector?, Selector> argument)
        {
            return new NotSelector(previous, argument(null));
        }
        
        /// <summary>
        /// Returns a selector which inverts the results of selector argument.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="argument">The selector to be not-ed.</param>
        /// <returns>The selector.</returns>
        public static Selector Not(this Selector? previous, Selector argument)
        {
            return new NotSelector(previous, argument);
        }

        /// <inheritdoc cref="NthChildSelector"/>
        /// <inheritdoc cref="NthChildSelector(Selector?, int, int)"/>
        /// <returns>The selector.</returns>
        public static Selector NthChild(this Selector? previous, int step, int offset)
        {
            return new NthChildSelector(previous, step, offset);
        }

        /// <inheritdoc cref="NthLastChildSelector"/>
        /// <inheritdoc cref="NthLastChildSelector(Selector?, int, int)"/>
        /// <returns>The selector.</returns>
        public static Selector NthLastChild(this Selector? previous, int step, int offset)
        {
            return new NthLastChildSelector(previous, step, offset);
        }

        /// <summary>
        /// Returns a selector which matches a type.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="type">The type.</param>
        /// <returns>The selector.</returns>
        public static Selector OfType(this Selector? previous, Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

            return TypeNameAndClassSelector.OfType(previous, type);
        }

        /// <summary>
        /// Returns a selector which matches a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector OfType<T>(this Selector? previous) where T : StyledElement
        {
            return previous.OfType(typeof(T));
        }

        /// <summary>
        /// Returns a selector which ORs selectors.
        /// </summary>
        /// <param name="selectors">The selectors to be OR'd.</param>
        /// <returns>The selector.</returns>
        public static Selector Or(params Selector[] selectors)
        {
            return new OrSelector(selectors);
        }

        /// <summary>
        /// Returns a selector which ORs selectors.
        /// </summary>
        /// <param name="selectors">The selectors to be OR'd.</param>
        /// <returns>The selector.</returns>
        public static Selector Or(IReadOnlyList<Selector> selectors)
        {
            return new OrSelector(selectors);
        }

        /// <summary>
        /// Returns a selector which matches a control with the specified property value.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <returns>The selector.</returns>
        public static Selector PropertyEquals<T>(this Selector? previous, AvaloniaProperty<T> property, object? value)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));

            return new PropertyEqualsSelector(previous, property, value);
        }

        /// <summary>
        /// Returns a selector which matches a control with the specified property value.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <returns>The selector.</returns>
        public static Selector PropertyEquals(this Selector? previous, AvaloniaProperty property, object? value)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));

            return new PropertyEqualsSelector(previous, property, value);
        }

        /// <summary>
        /// Returns a selector which enters a lookless control's template.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Template(this Selector previous)
        {
            return new TemplateSelector(previous);
        }
    }
}
