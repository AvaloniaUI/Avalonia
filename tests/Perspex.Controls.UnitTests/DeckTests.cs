// -----------------------------------------------------------------------
// <copyright file="DeckTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.LogicalTree;
    using Xunit;

    public class DeckTests
    {
        [Fact]
        public void First_Item_Should_Be_Selected_By_Default()
        {
            var target = new Deck
            {
                Template = new ControlTemplate<Deck>(this.CreateTemplate),
                Items = new[]
                {
                    "Foo",
                    "Bar"
                }
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
        }

        [Fact]
        public void LogicalChild_Should_Be_Selected_Item()
        {
            var target = new Deck
            {
                Template = new ControlTemplate<Deck>(this.CreateTemplate),
                Items = new[]
                {
                    "Foo",
                    "Bar"
                }
            };

            target.ApplyTemplate();

            Assert.Equal(1, target.GetLogicalChildren().Count());

            var child = target.GetLogicalChildren().Single();
            Assert.IsType<TextBlock>(child);
            Assert.Equal("Foo", ((TextBlock)child).Text);
        }

        private Control CreateTemplate(Deck control)
        {
            return new DeckPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = control[~Deck.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~Deck.ItemsPanelProperty],
                [~DeckPresenter.SelectedIndexProperty] = control[~Deck.SelectedIndexProperty],
                [~DeckPresenter.TransitionProperty] = control[~Deck.TransitionProperty],
            };
        }
    }
}
