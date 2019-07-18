// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reflection;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Builds a control for a piece of data.
    /// </summary>
    public class FuncDataTemplate : FuncTemplate<object, IControl>, IDataTemplate
    {
        /// <summary>
        /// The default data template used in the case where no matching data template is found.
        /// </summary>
        public static readonly FuncDataTemplate Default =
            new FuncDataTemplate<object>(
                (data, s) =>
                {
                    if (data != null)
                    {
                        var result = new TextBlock();
                        result.Bind(
                            TextBlock.TextProperty,
                            result.GetObservable(Control.DataContextProperty).Select(x => x?.ToString()));
                        return result;
                    }
                    else
                    {
                        return null;
                    }
                },
                true);

        /// <summary>
        /// The implementation of the <see cref="Match"/> method.
        /// </summary>
        private readonly Func<object, bool> _match;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataTemplate"/> class.
        /// </summary>
        /// <param name="type">The type of data which the data template matches.</param>
        /// <param name="build">
        /// A function which when passed an object of <paramref name="type"/> returns a control.
        /// </param>
        /// <param name="supportsRecycling">Whether the control can be recycled.</param>
        public FuncDataTemplate(
            Type type, 
            Func<object, INameScope, IControl> build,
            bool supportsRecycling = false)
            : this(o => IsInstance(o, type), build, supportsRecycling)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataTemplate"/> class.
        /// </summary>
        /// <param name="match">
        /// A function which determines whether the data template matches the specified data.
        /// </param>
        /// <param name="build">
        /// A function which returns a control for matching data.
        /// </param>
        /// <param name="supportsRecycling">Whether the control can be recycled.</param>
        public FuncDataTemplate(
            Func<object, bool> match,
            Func<object, INameScope, IControl> build,
            bool supportsRecycling = false)
            : base(build)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            _match = match;
            SupportsRecycling = supportsRecycling;
        }

        /// <inheritdoc/>
        public bool SupportsRecycling { get; }

        /// <summary>
        /// Checks to see if this data template matches the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// True if the data template can build a control for the data, otherwise false.
        /// </returns>
        public bool Match(object data)
        {
            return _match(data);
        }

        /// <summary>
        /// Determines of an object is of the specified type.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="t">The type.</param>
        /// <returns>
        /// True if <paramref name="o"/> is of type <paramref name="t"/>, otherwise false.
        /// </returns>
        private static bool IsInstance(object o, Type t)
        {
            return (o != null) && t.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo());
        }
    }
}
