using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    /// <summary>
    /// The PlatformHotkeyConfiguration class represents a configuration for platform-specific hotkeys in an Avalonia application.
    /// </summary>
    public sealed class PlatformHotkeyConfiguration
    {
        [PrivateApi]
        public static PlatformHotkeyConfiguration Apple
        {
            get
            {
                var hotkeys = new PlatformHotkeyConfiguration(KeyModifiers.Meta, wholeWordTextActionModifiers: KeyModifiers.Alt);

                hotkeys.MoveCursorToTheStartOfLine.Clear();
                hotkeys.MoveCursorToTheStartOfLine.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers));
                hotkeys.MoveCursorToTheStartOfLineWithSelection.Clear();
                hotkeys.MoveCursorToTheStartOfLineWithSelection.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
                hotkeys.MoveCursorToTheEndOfLine.Clear();
                hotkeys.MoveCursorToTheEndOfLine.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers));
                hotkeys.MoveCursorToTheEndOfLineWithSelection.Clear();
                hotkeys.MoveCursorToTheEndOfLineWithSelection.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
                hotkeys.MoveCursorToTheStartOfDocument.Clear();
                hotkeys.MoveCursorToTheStartOfDocument.Add(new KeyGesture(Key.Up, hotkeys.CommandModifiers));
                hotkeys.MoveCursorToTheStartOfDocumentWithSelection.Clear();
                hotkeys.MoveCursorToTheStartOfDocumentWithSelection.Add(new KeyGesture(Key.Up, KeyModifiers.Shift | hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
                hotkeys.MoveCursorToTheEndOfDocument.Clear();
                hotkeys.MoveCursorToTheEndOfDocument.Add(new KeyGesture(Key.Down, hotkeys.CommandModifiers));
                hotkeys.MoveCursorToTheEndOfDocumentWithSelection.Clear();
                hotkeys.MoveCursorToTheEndOfDocumentWithSelection.Add(new KeyGesture(Key.Down, KeyModifiers.Shift | hotkeys.CommandModifiers | hotkeys.SelectionModifiers));

                hotkeys.ScrollToTheStartOfDocument.Add(new KeyGesture(Key.Home));
                hotkeys.ScrollToTheEndOfDocument.Add(new KeyGesture(Key.End));
                hotkeys.ExtendSelectionToTheStartOfDocument.Add(new KeyGesture(Key.Home, KeyModifiers.Shift | hotkeys.SelectionModifiers));
                hotkeys.ExtendSelectionToTheEndOfDocument.Add(new KeyGesture(Key.End, KeyModifiers.Shift | hotkeys.SelectionModifiers));

                return hotkeys;
            }
        }

        [PrivateApi]
        public PlatformHotkeyConfiguration() : this(KeyModifiers.Control)
        {

        }

        [PrivateApi]
        public PlatformHotkeyConfiguration(KeyModifiers commandModifiers,
            KeyModifiers selectionModifiers = KeyModifiers.Shift,
            KeyModifiers wholeWordTextActionModifiers = KeyModifiers.Control)
        {
            CommandModifiers = commandModifiers;
            SelectionModifiers = selectionModifiers;
            WholeWordTextActionModifiers = wholeWordTextActionModifiers;
            Copy = new List<KeyGesture>
            {
                new KeyGesture(Key.C, commandModifiers),
                new KeyGesture(Key.Insert, KeyModifiers.Control)
            };
            Cut = new List<KeyGesture>
            {
                new KeyGesture(Key.X, commandModifiers)
            };
            Paste = new List<KeyGesture>
            {
                new KeyGesture(Key.V, commandModifiers),
                new KeyGesture(Key.Insert, KeyModifiers.Shift)
            };
            Undo = new List<KeyGesture>
            {
                new KeyGesture(Key.Z, commandModifiers)
            };
            Redo = new List<KeyGesture>
            {
                new KeyGesture(Key.Y, commandModifiers),
                new KeyGesture(Key.Z, commandModifiers | selectionModifiers)
            };
            SelectAll = new List<KeyGesture>
            {
                new KeyGesture(Key.A, commandModifiers)
            };
            MoveCursorToTheStartOfLine = new List<KeyGesture>
            {
                new KeyGesture(Key.Home)
            };
            MoveCursorToTheEndOfLine = new List<KeyGesture>
            {
                new KeyGesture(Key.End)
            };
            MoveCursorToTheStartOfDocument = new List<KeyGesture>
            {
                new KeyGesture(Key.Home, commandModifiers)
            };
            MoveCursorToTheEndOfDocument = new List<KeyGesture>
            {
                new KeyGesture(Key.End, commandModifiers)
            };
            MoveCursorToTheStartOfLineWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.Home, selectionModifiers)
            };
            MoveCursorToTheEndOfLineWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.End, selectionModifiers)
            };
            MoveCursorToTheStartOfDocumentWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.Home, commandModifiers | selectionModifiers)
            };
            MoveCursorToTheEndOfDocumentWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.End, commandModifiers | selectionModifiers)
            };
            ScrollToTheStartOfDocument = new List<KeyGesture>();
            ScrollToTheEndOfDocument = new List<KeyGesture>();
            ExtendSelectionToTheStartOfDocument = new List<KeyGesture>();
            ExtendSelectionToTheEndOfDocument = new List<KeyGesture>();
            OpenContextMenu = new List<KeyGesture>
            {
                new KeyGesture(Key.Apps)
            };
            Back = new List<KeyGesture>
            {
                new KeyGesture(Key.Left, KeyModifiers.Alt)
            };
            PageLeft = new List<KeyGesture>
            {
                new KeyGesture(Key.PageUp, KeyModifiers.Shift)
            };
            PageRight = new List<KeyGesture>
            {
                new KeyGesture(Key.PageDown, KeyModifiers.Shift)
            };
            PageUp = new List<KeyGesture>
            {
                new KeyGesture(Key.PageUp)
            };
            PageDown = new List<KeyGesture>
            {
                new KeyGesture(Key.PageDown)
            };
        }

        public KeyModifiers CommandModifiers { get; set; }
        public KeyModifiers WholeWordTextActionModifiers { get; set; }
        public KeyModifiers SelectionModifiers { get; set; }
        public List<KeyGesture> Copy { get; set; }
        public List<KeyGesture> Cut { get; set; }
        public List<KeyGesture> Paste { get; set; }
        public List<KeyGesture> Undo { get; set; }
        public List<KeyGesture> Redo { get; set; }
        public List<KeyGesture> SelectAll { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfLine { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfLine { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfDocument { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfDocument { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfLineWithSelection { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfLineWithSelection { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfDocumentWithSelection { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfDocumentWithSelection { get; set; }
        public List<KeyGesture> OpenContextMenu { get; set; }
        public List<KeyGesture> Back { get; set; }
        public List<KeyGesture> PageUp { get; set; }
        public List<KeyGesture> PageDown { get; set; }
        public List<KeyGesture> PageRight { get; set; }
        public List<KeyGesture> PageLeft { get; set; }
        // These explicitly do not alter caret position or selection. They are a standard Apple ecosystem behaviour.
        public List<KeyGesture> ScrollToTheStartOfDocument { get; set; }
        public List<KeyGesture> ScrollToTheEndOfDocument { get; set; }
        // These extend selection relative to the selection bundaries, irrespective of direction.
        public List<KeyGesture> ExtendSelectionToTheStartOfDocument { get; set; }
        public List<KeyGesture> ExtendSelectionToTheEndOfDocument { get; set; }
    }
}
