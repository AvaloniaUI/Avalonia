namespace Avalonia.Controls
{
    // Note: FlyoutShowMode.Auto was removed. MS Docs just say:
    // The show mode is determined automatically based on the method used to show the flyout.
    // and AFAICT Flyouts generally open with "Standard" behavior

    public enum FlyoutShowMode
    {
        /// <summary>
        /// Behavior is typical of a flyout shown reactively, like a context menu. The open flyout takes focus. For a CommandBarFlyout, it opens in it's expanded state.
        /// </summary>
        Standard,

        /// <summary>
        /// Behavior is typical of a flyout shown proactively. The open flyout does not take focus.
        /// </summary>
        Transient,

        /// <summary>
        /// The flyout exhibits Transient behavior while the cursor is close to it, but is dismissed when the cursor moves away.
        /// </summary>
        TransientWithDismissOnPointerMoveAway
    }
}
