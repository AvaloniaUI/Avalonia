namespace Avalonia.Layout
{
    /// <summary>
    /// Defines how a container is queried.
    /// </summary>
    public enum ContainerType
    {
        /// <summary>
        /// The container will not be queries for any container size queries.
        /// </summary>
        Normal,

        /// <summary>
        /// The container can be queried for container size queries for width.
        /// </summary>
        Width,

        /// <summary>
        /// The can be queried for container size queries for height.
        /// </summary>
        Height,

        /// <summary>
        /// The can be queried for container size queries for both width and height.
        /// </summary>
        WidthAndHeight
    }
}
