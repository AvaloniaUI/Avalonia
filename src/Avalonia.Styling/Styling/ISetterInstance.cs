#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a setter that has been instanced on a control.
    /// </summary>
    public interface ISetterInstance
    {
        /// <summary>
        /// Activates the setter.
        /// </summary>
        public void Activate();

        /// <summary>
        /// Deactivates the setter.
        /// </summary>
        public void Deactivate();
    }
}
