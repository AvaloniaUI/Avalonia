// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.OpenGL
{
    public class GlRequest
    {
        /// <summary>
        /// OpenGL API to use
        /// </summary>
        public GlApi Api { get; set; }

        /// <summary>
        /// OpenGL version to use
        /// </summary>
        public GlVersion Version { get; set; }

        /// <summary>
        /// Leave this unset if the GLVersion used is Latest
        /// </summary>
        public int GlMajor { get; set; }

        /// <summary>
        /// Leave this unset if the GlVersion used is Latest
        /// </summary>
        public int GlMinor { get; set; }
    }

    public enum GlApi
    {
        /// <summary>
        /// Tries GL and if that fails tries GLES
        /// </summary>
        Auto,

        /// <summary>
        /// Tries GL
        /// </summary>
        Gl,

        /// <summary>
        /// Tries GLES
        /// </summary>
        Gles
    }

    public enum GlVersion
    {
        /// <summary>
        /// Use the lates GL version 
        /// </summary>
        Latest,

        /// <summary>
        /// Use a specific GL version
        /// </summary>
        Specific
    }
}