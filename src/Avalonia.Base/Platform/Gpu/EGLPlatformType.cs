namespace Avalonia.Platform.Gpu
{
    /// <summary>
    /// EGL platform types.
    /// </summary>
    public enum EGLPlatformType
    {
        /// <summary>
        /// Default for current OS.
        /// </summary>
        Default,

        /// <summary>
        /// Direct3D 9
        /// </summary>
        D3D9,

        /// <summary>
        /// Direct3D 11
        /// </summary>
        D3D11,

        /// <summary>
        /// OpenGL
        /// </summary>
        OpenGL,

        /// <summary>
        /// OpenGL ES
        /// </summary>
        OpenGL_ES
    }
}