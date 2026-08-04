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
        public void BeginBatchUpdate_Is_A_Flag_Not_A_Counter_So_A_Nested_End_Releases_The_Outer_Batch()
        {
            // DEFECT (documented, not fixed): _deferUpdateChild is a bool, so Begin/End do not
            // nest. An inner EndBatchUpdate publishes the outer batch's half-finished state and
            // leaves the outer batch inoperative. Nothing in the current call sites nests, so
            // this is latent - but the contract is unsound.
            var template = new CountingTemplate("A");
            var target = CreateRooted(template, "foo");

            target.BeginBatchUpdate();      // outer
            target.Content = "bar";
            target.BeginBatchUpdate();      // inner: no-op, the flag is already set
            target.EndBatchUpdate();        // inner: publishes the outer batch's pending change

            Assert.Equal(2, template.BuildCount);
            Assert.Equal("A:bar", TagOf(target.Child));

            // The outer batch is no longer in effect, so further changes apply immediately.
            target.Content = "baz";
            Assert.Equal(3, template.BuildCount);

            // ...and the outer End is now just an extra rebuild.
            target.EndBatchUpdate();
            Assert.Equal(4, template.BuildCount);
        }

        [Fact]
        public void EndBatchUpdate_Without_A_Matching_Begin_Rebuilds_The_Child()
        {
            // DEFECT (documented, not fixed): EndBatchUpdate is not a no-op when no batch is
            // open - it always calls UpdateChild, which rebuilds the child for nothing. It does
            // not throw, so an unbalanced End is "safe" but wasteful.
            var template = new CountingTemplate("A");
            var target = CreateRooted(template, "foo");
            var childBefore = target.Child;

            target.EndBatchUpdate();

            Assert.Equal(2, template.BuildCount);
            Assert.NotSame(childBefore, target.Child);
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
        public void A_Batch_Does_Not_Refresh_The_Empty_PseudoClass()
        {
            // DEFECT (documented, not fixed): ContentChanged returns before UpdatePseudoClasses
            // when a batch is open, and EndBatchUpdate only calls UpdateChild. So ':empty' keeps
            // the value it had before the batch. Every ContentPresenter container prepared by
            // ItemsControl.PrepareContainerForItemOverride takes this path, which means such a
            // container carries ':empty' for its whole life despite having content.
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

            // Correct behaviour would be DoesNotContain - this pins what actually happens.
            Assert.True(target.Classes.Contains(":empty"));

            // A subsequent change outside a batch heals it.
            target.Content = "bar";
            Assert.False(target.Classes.Contains(":empty"));
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
