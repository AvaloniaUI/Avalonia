using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a composite page transition that can be used to combine multiple transitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instantiate the <see cref="CompositePageTransition" /> in XAML and initialize the
    /// <see cref="Transitions" /> property in order to have many animations triggered at once.
    /// For example, you can combine <see cref="CrossFade"/> and <see cref="PageSlide"/>.
    /// <code>
    /// <![CDATA[
    /// <reactiveUi:RoutedViewHost Router="{Binding Router}">
    ///   <reactiveUi:RoutedViewHost.PageTransition>
    ///     <CompositePageTransition>
    ///       <PageSlide Duration="0.5" />
    ///       <CrossFade Duration="0.5" />
    ///     </CompositePageTransition>
    ///   </reactiveUi:RoutedViewHost.PageTransition>
    /// </reactiveUi:RoutedViewHost>
    /// ]]>
    /// </code>
    /// </para>
    /// </remarks>
    public class CompositePageTransition : IPageTransition
    {
        /// <summary>
        /// Gets or sets the transitions to be executed. Can be defined from XAML.
        /// </summary>
        [Content]
        public List<IPageTransition> PageTransitions { get; set; } = new List<IPageTransition>();

        /// <inheritdoc />
        public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            var transitionTasks = PageTransitions
                .Select(transition => transition.Start(from, to, forward, cancellationToken))
                .ToArray();
            return Task.WhenAll(transitionTasks);
        }
    }
}
