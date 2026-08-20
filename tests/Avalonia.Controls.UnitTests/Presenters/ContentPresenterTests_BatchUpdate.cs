using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Presenters
{
    /// <summary>
    /// Tests for <c>ContentPresenter.BeginBatchUpdate</c> / <c>EndBatchUpdate</c>.
    /// <para>
    /// <c>ItemsControl.PrepareContainerForItemOverride</c> wraps its <c>Content</c> +
    /// <c>ContentTemplate</c> assignment in a batch so the pair lands together, instead of
    /// running <c>UpdateChild</c> once with a mismatched pair and rebuilding the child for
    /// nothing. The batch flag makes <c>ContentChanged</c> return early, which is exactly where
    /// a missed <c>UpdateChild</c> would leave a blank row.
    /// </para>
    /// </summary>
    public class ContentPresenterTests_BatchUpdate : ScopedTestBase
    {
        [Fact]
        public void Content_Set_Inside_A_Batch_Does_Not_Update_The_Child_Until_EndBatchUpdate()
        {
            var template = new CountingTemplate("A");
            var target = CreateRooted(template, "foo");

            var childBeforeBatch = target.Child;
            Assert.Equal(1, template.BuildCount);
            Assert.Equal("A:foo", TagOf(childBeforeBatch));

            target.BeginBatchUpdate();
            target.Content = "bar";

            // Nothing has happened yet: same child instance, still showing the old content.
            Assert.Same(childBeforeBatch, target.Child);
            Assert.Equal(1, template.BuildCount);
            Assert.Equal("A:foo", TagOf(target.Child));

            target.EndBatchUpdate();

            Assert.Equal(2, template.BuildCount);
            Assert.NotSame(childBeforeBatch, target.Child);
            Assert.Equal("A:bar", TagOf(target.Child));
        }

        [Fact]
        public void EndBatchUpdate_Applies_A_Content_And_Template_Set_During_The_Batch()
        {
            // The blank-row regression: a recycled container has its Content and ContentTemplate
            // assigned inside a batch. If EndBatchUpdate does not apply them, the presenter is
            // left with no Child at all.
            var target = new ContentPresenter();
            _ = new TestRoot(target);
            target.ApplyTemplate();
            Assert.Null(target.Child);

            var template = new CountingTemplate("A");

            target.BeginBatchUpdate();
            target.Content = "foo";
            target.ContentTemplate = template;

            // Still blank while the batch is open.
            Assert.Null(target.Child);
            Assert.Equal(0, template.BuildCount);

            target.EndBatchUpdate();

            Assert.NotNull(target.Child);
            Assert.Equal("A:foo", TagOf(target.Child));
            Assert.Equal(1, template.BuildCount);
        }

        [Fact]
        public void Content_And_ContentTemplate_In_One_Batch_Build_The_Child_Once_From_The_New_Pair()
        {
            var oldTemplate = new CountingTemplate("A");
            var newTemplate = new CountingTemplate("B");
            var target = CreateRooted(oldTemplate, "foo");

            target.BeginBatchUpdate();
            target.Content = "bar";
            target.ContentTemplate = newTemplate;
            target.EndBatchUpdate();

            // Exactly one build for the whole batch, by the new template from the new content.
            Assert.Equal(1, newTemplate.BuildCount);
            Assert.Equal("B:bar", TagOf(target.Child));

            // And never a mismatched pair: the old template was never asked to build the new
            // content, and the new template was never asked to build the old content.
            Assert.Equal(new object?[] { "foo" }, oldTemplate.BuiltFor);
            Assert.Equal(new object?[] { "bar" }, newTemplate.BuiltFor);
        }

        [Fact]
        public void Without_A_Batch_Content_And_ContentTemplate_Build_The_Child_Twice_From_A_Mismatched_Pair()
        {
            // The control case that justifies the batch: assigned separately, the old template
            // is first asked to build the new content, and the child is built twice.
            var oldTemplate = new CountingTemplate("A");
            var newTemplate = new CountingTemplate("B");
            var target = CreateRooted(oldTemplate, "foo");

            target.Content = "bar";
            target.ContentTemplate = newTemplate;

            Assert.Equal(new object?[] { "foo", "bar" }, oldTemplate.BuiltFor);
            Assert.Equal(new object?[] { "bar" }, newTemplate.BuiltFor);
            Assert.Equal("B:bar", TagOf(target.Child));
        }

        [Fact]
        public void Batches_Nest_So_Only_The_Outermost_End_Publishes()
        {
            // The batch is a depth counter, not a flag: an inner End must leave the outer batch in
            // effect, or a nested call site would publish half of the outer batch's state.
            var template = new CountingTemplate("A");
            var target = CreateRooted(template, "foo");

            target.BeginBatchUpdate();      // outer
            target.Content = "bar";
            target.BeginBatchUpdate();      // inner
            target.EndBatchUpdate();        // inner: the outer batch is still open

            Assert.Equal(1, template.BuildCount);
            Assert.Equal("A:foo", TagOf(target.Child));

            // Still batched, so a further change stays pending too.
            target.Content = "baz";
            Assert.Equal(1, template.BuildCount);

            // Only the outermost End applies the whole batch, and it builds once.
            target.EndBatchUpdate();
            Assert.Equal(2, template.BuildCount);
            Assert.Equal("A:baz", TagOf(target.Child));
        }

        [Fact]
        public void EndBatchUpdate_Without_A_Matching_Begin_Does_Nothing()
        {
            // An unbalanced End must not rebuild the child: PrepareContainerForItemOverride calls
            // End on every prepared container, and a presenter that never opened a batch would
            // otherwise pay a rebuild for nothing.
            var template = new CountingTemplate("A");
            var target = CreateRooted(template, "foo");
            var childBefore = target.Child;

            target.EndBatchUpdate();

            Assert.Equal(1, template.BuildCount);
            Assert.Same(childBefore, target.Child);
            Assert.Equal("A:foo", TagOf(target.Child));
        }

        [Fact]
        public void A_Batch_Left_Open_Strands_The_Child_Until_The_Next_Measure()
        {
            var template = new CountingTemplate("A");
            var target = CreateRooted(template, "foo");

            target.BeginBatchUpdate();
            target.Content = "bar";

            // Stranded on the old content for as long as nothing measures the presenter. Note
            // that ContentChanged returns before InvalidateMeasure too, so nothing schedules
            // that measure by itself.
            Assert.Equal("A:foo", TagOf(target.Child));
            Assert.Equal(1, template.BuildCount);

            // ApplyTemplate does not consult the batch flag: it only looks at _createdChild,
            // which ContentChanged cleared. So the next measure rebuilds the child even though
            // the batch is still open - an unclosed batch self-heals rather than blanking out.
            target.Measure(new Size(100, 100));

            Assert.Equal(2, template.BuildCount);
            Assert.Equal("A:bar", TagOf(target.Child));
        }

        [Fact]
        public void A_Batch_Refreshes_The_Empty_PseudoClass_When_It_Ends()
        {
            // ContentChanged returns before UpdatePseudoClasses while a batch is open, so
            // EndBatchUpdate has to refresh them for the whole batch. Every ContentPresenter
            // container prepared by ItemsControl.PrepareContainerForItemOverride takes this path -
            // without the refresh such a container would carry ':empty' for its whole life while
            // holding content.
            var target = new ContentPresenter();
            _ = new TestRoot(target);
            target.ApplyTemplate();
            Assert.True(target.Classes.Contains(":empty"));

            var template = new CountingTemplate("A");
            target.BeginBatchUpdate();
            target.Content = "foo";
            target.ContentTemplate = template;
            target.EndBatchUpdate();

            Assert.NotNull(target.Child);
            Assert.False(target.Classes.Contains(":empty"));

            // ...and a change outside a batch keeps it in step, as before.
            target.Content = null;
            Assert.True(target.Classes.Contains(":empty"));
        }

        private static ContentPresenter CreateRooted(IDataTemplate? template, object? content)
        {
            // Content assigned while unrooted doesn't build a child, so the initial build is
            // driven explicitly and the build count starts from a known state.
            var target = new ContentPresenter { ContentTemplate = template, Content = content };
            _ = new TestRoot(target);
            target.UpdateChild();
            return target;
        }

        private static string? TagOf(Control? child) => (child as Canvas)?.Tag as string;

        /// <summary>
        /// A plain <see cref="IDataTemplate"/> - deliberately not an
        /// <see cref="IRecyclingDataTemplate"/> - so that every <c>UpdateChild</c> is observable
        /// as a fresh build. The built control is tagged with the template that made it and the
        /// data it was made from, which is what makes a mismatched pair detectable.
        /// </summary>
        private class CountingTemplate : IDataTemplate
        {
            private readonly string _tag;

            public CountingTemplate(string tag) => _tag = tag;

            public List<object?> BuiltFor { get; } = new();

            public int BuildCount => BuiltFor.Count;

            public bool Match(object? data) => true;

            public Control Build(object? data)
            {
                BuiltFor.Add(data);
                return new Canvas { Tag = $"{_tag}:{data}", Width = 10, Height = 10 };
            }
        }
    }
}
