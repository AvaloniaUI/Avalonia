// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia context menu for core.
    /// </summary>
    internal sealed class NullContextMenuAdapter : RContextMenu
    {
        //TODO: actually implement context menu

        private int _itemCount;
        public override int ItemsCount => _itemCount;
        public override void AddDivider()
        {
            
        }

        public override void AddItem(string text, bool enabled, EventHandler onClick)
        {
            _itemCount++;
        }

        public override void RemoveLastDivider()
        {
            _itemCount++;
        }

        public override void Show(RControl parent, RPoint location)
        {
        }

        public override void Dispose()
        {
        }
    }
}