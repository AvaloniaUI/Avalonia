// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.
// Copied from: https://github.com/toptensoftware/RichTextKit

namespace Avalonia.Media.Text.Unicode
{
    /// <summary>
    /// Unicode directionality classes
    /// </summary>
    /// <remarks>
    /// Note, these need to match those used by the JavaScript script that
    /// generates the .trie resources
    /// </remarks>
    internal enum Directionality : byte
    {
        // Strong types
        L = 0,
        R = 1,
        AL = 2,

        // Weak Types
        EN = 3,
        ES = 4,
        ET = 5,
        AN = 6,
        CS = 7,
        NSM = 8,
        BN = 9,

        // Neutral Types
        B = 10,
        S = 11,
        WS = 12,
        ON = 13,

        // Explicit Formatting Types - Embed
        LRE = 14,
        LRO = 15,
        RLE = 16,
        RLO = 17,
        PDF = 18,

        // Explicit Formatting Types - Isolate
        LRI = 19,
        RLI = 20,
        FSI = 21,
        PDI = 22,

        /** Minimum bidi type value. */
        TYPE_MIN = 0,

        /** Maximum bidi type value. */
        TYPE_MAX = 22,

        /* Unknown */
        Unknown = 0xFF,
    }

}
