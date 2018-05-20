// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Gpu
{
    /// <summary>
    /// Hooks for modifying EGL behavior.
    /// </summary>
    public interface IEGLPlatformHooks
    {
        /// <summary>
        /// Allows for modyfing platform type.
        /// </summary>
        /// <param name="platformType">Platform type about to be used.</param>
        void InspectPlatformType(ref EGLPlatformType platformType);
        
        /// <summary>
        /// Inspect version of EGL.
        /// </summary>
        /// <param name="majorVersion">Major version.</param>
        /// <param name="minorVersion">Minor version.</param>
        void InspectVersion(int majorVersion, int minorVersion);
    }
}