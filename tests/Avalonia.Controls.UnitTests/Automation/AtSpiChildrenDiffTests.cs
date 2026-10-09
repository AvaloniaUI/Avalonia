using System.Collections.Generic;
using Avalonia.FreeDesktop.AtSpi;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation
{
    public class AtSpiChildrenDiffTests : ScopedTestBase
    {
        [Fact]
        public void Replaced_Child_Is_Removed_Then_Added_At_Index_0()
        {
            var (removed, added) = Diff(["old pane"], ["new pane"]);

            Assert.Equal([(0, "old pane")], removed);
            Assert.Equal([(0, "new pane")], added);
        }

        [Fact]
        public void Unchanged_Children_Report_Nothing()
        {
            var (removed, added) = Diff(["a", "b", "c"], ["a", "b", "c"]);

            Assert.Empty(removed);
            Assert.Empty(added);
        }

        [Fact]
        public void Removed_Children_Use_Old_Indices_Highest_First()
        {
            var (removed, added) = Diff(["a", "b", "c", "d"], ["a", "c"]);

            Assert.Equal([(3, "d"), (1, "b")], removed);
            Assert.Empty(added);
        }

        [Fact]
        public void Added_Children_Use_New_Indices_Lowest_First()
        {
            var (removed, added) = Diff(["b"], ["a", "b", "c"]);

            Assert.Empty(removed);
            Assert.Equal([(0, "a"), (2, "c")], added);
        }

        [Fact]
        public void First_Children_Are_All_Added()
        {
            var (removed, added) = Diff([], ["a", "b"]);

            Assert.Empty(removed);
            Assert.Equal([(0, "a"), (1, "b")], added);
        }

        [Fact]
        public void Moved_Child_Is_Removed_And_Added()
        {
            var (removed, added) = Diff(["a", "b", "c"], ["c", "a", "b"]);

            Assert.Equal([(2, "c")], removed);
            Assert.Equal([(0, "c")], added);
        }

        [Theory]
        [InlineData(new[] { "a", "b", "c" }, new[] { "c", "b", "a" })]
        [InlineData(new[] { "a", "b", "c", "d" }, new[] { "x", "b", "y", "d", "a" })]
        [InlineData(new[] { "a", "b" }, new string[0])]
        [InlineData(new[] { "a", "b", "c" }, new[] { "b", "d", "a", "c", "e" })]
        public void Applying_Changes_In_Order_Produces_New_Children(string[] oldChildren, string[] newChildren)
        {
            var (removed, added) = Diff(oldChildren, newChildren);
            var children = new List<string>(oldChildren);

            foreach (var (index, child) in removed)
            {
                Assert.Equal(child, children[index]);
                children.RemoveAt(index);
            }

            foreach (var (index, child) in added)
                children.Insert(index, child);

            Assert.Equal(newChildren, children);
        }

        private static (List<(int Index, string Child)> Removed, List<(int Index, string Child)> Added) Diff(
            string[] oldChildren,
            string[] newChildren)
        {
            var removed = new List<(int Index, string Child)>();
            var added = new List<(int Index, string Child)>();
            AtSpiNode.DiffChildren(oldChildren, newChildren, removed, added);
            return (removed, added);
        }
    }
}
