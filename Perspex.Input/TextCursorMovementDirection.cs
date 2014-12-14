namespace Perspex.Input
{
    /// <summary>
    /// Describes how focus should be moved.
    /// </summary>
    public enum TextCursorMovementDirection
    {      
        /// <summary>
        /// Moves the cursor to the left.
        /// </summary>
        Left,

        /// <summary>
        /// Moves the cursor to the right.
        /// </summary>
        Right,

        /// <summary>
        /// Moves the cursor up.
        /// </summary>
        Up,

        /// <summary>
        /// Moves the cursor down.
        /// </summary>
        Down,

        /// <summary>
        /// Moves the cursor to the beginning.
        /// </summary>
        Beginning,

        /// <summary>
        /// Moves the the end.
        /// </summary>
        End,

        EndOfLine,
        BeginingOfLine,
        Begining
    }
}