using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// Picks a row template by the row's type, and tells the virtualizing panel how to pool the
    /// controls it builds.
    ///
    /// A flat list of heterogeneous rows cannot use one template, and a plain
    /// <c>ItemsControl.DataTemplates</c> collection would recycle nothing: every scroll step would
    /// rebuild a row's whole control tree. Implementing <see cref="IVirtualizingDataTemplate"/>
    /// instead gives the panel a recycle key per row kind, so a container built for one image row
    /// is reused for the next image row and only its bindings change.
    /// </summary>
    /// <remarks>
    /// Templates are supplied as content, so the layouts stay in XAML next to the page:
    /// <code>
    /// &lt;pages:FieldTemplateSelector x:Key="FieldTemplates"&gt;
    ///   &lt;DataTemplate DataType="vm:HeadlineItem"&gt;…&lt;/DataTemplate&gt;
    ///   &lt;DataTemplate DataType="vm:TextFieldItem"&gt;…&lt;/DataTemplate&gt;
    /// &lt;/pages:FieldTemplateSelector&gt;
    /// </code>
    /// </remarks>
    public class FieldTemplateSelector : IVirtualizingDataTemplate
    {
        [Content]
        public List<IDataTemplate> Templates { get; } = new();

        /// <summary>
        /// Whether a recycled container may keep the control tree it already has.
        ///
        /// Turn it off to see what a plain <see cref="IDataTemplate"/> costs. A template that is not
        /// an <see cref="IRecyclingDataTemplate"/> — or that ignores the control handed back to it,
        /// which is what this flag simulates — makes <c>ContentPresenter</c> throw the row's control
        /// tree away and build a new one every time a container is reused for a different row. The
        /// container pooling still happens; it is the contents that get rebuilt.
        /// </summary>
        public bool RecycleContent { get; set; } = true;

        /// <summary>
        /// How many row control trees have been built from scratch. With recycling on this settles
        /// at roughly the number of containers in play; with it off it climbs for as long as you
        /// keep scrolling.
        /// </summary>
        public int Builds { get; private set; }

        public void ResetBuilds() => Builds = 0;

        /// <summary>Upper bound on idle containers kept per row kind.</summary>
        public int MaxPoolSizePerKey { get; set; } = 6;

        /// <summary>
        /// How many containers warmup pre-builds per row kind. Warmup grows the pool off the row
        /// kinds the panel has actually met, so a kind that first appears deep in the list (the
        /// markdown and image rows here) is covered when the user reaches it rather than only if it
        /// happened to occur near the top.
        /// </summary>
        public int MinPoolSizePerKey { get; set; } = 3;

        /// <summary>
        /// The recycle key. Row kind is the right granularity: two image rows have the same control
        /// tree and differ only in their data, whereas an image row and a number field have nothing
        /// in common and must never be swapped for one another.
        /// </summary>
        public object? GetKey(object? data) => data?.GetType();

        public bool Match(object? data) => FindTemplate(data) is not null;

        public Control? Build(object? data)
        {
            var built = FindTemplate(data)?.Build(data);
            if (built is not null)
                Builds++;
            return built;
        }

        public Control? Build(object? data, Control? existing)
        {
            // The control tree handed back is already the right one for this row kind (the panel
            // only offers a container whose recycle key matches), so keep it and let the
            // DataContext change drive the update. Rebuilding here is what a plain IDataTemplate
            // effectively does, and it is the expensive path — see RecycleContent.
            if (existing is not null && RecycleContent)
                return existing;

            return Build(data);
        }

        private IDataTemplate? FindTemplate(object? data)
        {
            if (data is null)
                return null;

            foreach (var template in Templates)
            {
                if (template.Match(data))
                    return template;
            }

            return null;
        }
    }
}
