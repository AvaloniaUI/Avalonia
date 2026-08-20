using System;
using Avalonia.Reactive;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Builds a control for a piece of data.
    /// </summary>
    public class FuncDataTemplate : FuncTemplate<object?, Control?>, IVirtualizingDataTemplate
    {
        /// <summary>
        /// The default data template used in the case where no matching data template is found.
        /// </summary>
        public static readonly FuncDataTemplate Default =
            new FuncDataTemplate<object?>(
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
        /// The default data template used in the case where no matching data template is found
        /// but <see cref="AccessText"/> should be used.
        /// </summary>
        public static readonly FuncDataTemplate Access =
            new FuncDataTemplate<object>(
                (data, s) =>
                {
                    if (data != null)
                    {
                        var result = new AccessText();
                        result.Bind(TextBlock.TextProperty,
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
        private readonly Func<object?, bool> _match;
        private readonly bool _supportsRecycling;

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
            Func<object?, INameScope, Control?> build,
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
            Func<object?, bool> match,
            Func<object?, INameScope, Control?> build,
            bool supportsRecycling = false)
            : base(build)
        {
            _match = match ?? throw new ArgumentNullException(nameof(match));
            _supportsRecycling = supportsRecycling;
        }

        /// <summary>
        /// Gets or sets the function that assigns a piece of data to a container recycling pool,
        /// opting this template into container-level virtualization.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Null by default, which means no opt-in: the template behaves exactly as it always has.
        /// Setting it is the code equivalent of <c>EnableVirtualization="True"</c> on a XAML
        /// <c>DataTemplate</c>, and containers built for data with the same key are pooled together
        /// with their child still attached.
        /// </para>
        /// <para>
        /// The key must identify the *shape* the build function produced, not merely the data's
        /// type: a template that branches on a property to build different subtrees has to key on
        /// that property (<c>d => ((Row)d!).Kind</c>), or a container built for one shape will be
        /// handed data of another and display the wrong tree. Where the build function does not
        /// branch, <c>d => d?.GetType()</c> is the natural choice.
        /// </para>
        /// <para>
        /// Returning null for a particular piece of data opts that data out again, and it falls
        /// back to stock recycling.
        /// </para>
        /// </remarks>
        public Func<object?, object?>? RecycleKeySelector { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of containers to pool per recycle key. Only consulted
        /// once <see cref="RecycleKeySelector"/> has opted this template in.
        /// </summary>
        public int MaxPoolSizePerKey { get; set; } = 5;

        /// <summary>
        /// Gets or sets the number of containers per key that warmup pre-builds, when warmup is
        /// enabled on the panel. Only consulted once <see cref="RecycleKeySelector"/> has opted this
        /// template in.
        /// </summary>
        public int MinPoolSizePerKey { get; set; } = 2;

        /// <summary>
        /// Checks to see if this data template matches the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// True if the data template can build a control for the data, otherwise false.
        /// </returns>
        public bool Match(object? data)
        {
            return _match(data);
        }

        /// <inheritdoc/>
        public object? GetKey(object? data) => RecycleKeySelector?.Invoke(data);

        /// <summary>
        /// Creates or recycles a control to display the specified data.
        /// </summary>
        /// <param name="data">The data to display.</param>
        /// <param name="existing">An optional control to recycle.</param>
        /// <returns>
        /// The <paramref name="existing"/> control if supplied and applicable to
        /// <paramref name="data"/>, otherwise a new control or null.
        /// </returns>
        /// <remarks>
        /// The caller should ensure that any control passed to <paramref name="existing"/>
        /// originated from the same data template.
        /// </remarks>
        public Control? Build(object? data, Control? existing)
        {
            // A template that opted into container-level virtualization must return the existing
            // child, or the pooling buys nothing: the panel would keep handing back a container
            // whose subtree this method then threw away and rebuilt.
            var reuse = _supportsRecycling || RecycleKeySelector is not null;

            return reuse && existing is object ? existing : Build(data);
        }

        /// <summary>
        /// Determines of an object is of the specified type.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="t">The type.</param>
        /// <returns>
        /// True if <paramref name="o"/> is of type <paramref name="t"/>, otherwise false.
        /// </returns>
        private static bool IsInstance(object? o, Type t)
        {
            return t.IsInstanceOfType(o);
        }
    }
}
