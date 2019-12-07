﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.Media
{
    /// <summary>
    ///     Represents a sequence of glyphs from a single face of a single font at a single size, and with a single rendering style.
    /// </summary>
    public sealed class GlyphRun : IDisposable
    {
        private static readonly IPlatformRenderInterface s_platformRenderInterface =
            AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

        private IGlyphRunImpl _glyphRunImpl;
        private GlyphTypeface _glyphTypeface;
        private double _fontRenderingEmSize;
        private Rect? _bounds;

        private ReadOnlySlice<ushort> _glyphIndices;
        private ReadOnlySlice<double> _glyphAdvances;
        private ReadOnlySlice<Vector> _glyphOffsets;
        private ReadOnlySlice<ushort> _glyphClusters;
        private ReadOnlySlice<char> _characters;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlyphRun"/> class.
        /// </summary>
        public GlyphRun()
        {

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlyphRun"/> class by specifying properties of the class.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="fontRenderingEmSize">The rendering em size.</param>
        /// <param name="glyphIndices">The glyph indices.</param>
        /// <param name="glyphAdvances">The glyph advances.</param>
        /// <param name="glyphOffsets">The glyph offsets.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="glyphClusters">The glyph clusters.</param>
        /// <param name="bidiLevel">The bidi level.</param>
        /// <param name="bounds">The bound.</param>
        public GlyphRun(
            GlyphTypeface glyphTypeface,
            double fontRenderingEmSize,
            ReadOnlySlice<ushort> glyphIndices,
            ReadOnlySlice<double> glyphAdvances = default,
            ReadOnlySlice<Vector> glyphOffsets = default,
            ReadOnlySlice<char> characters = default,
            ReadOnlySlice<ushort> glyphClusters = default,
            int bidiLevel = 0,
            Rect? bounds = null)
        {
            GlyphTypeface = glyphTypeface;

            FontRenderingEmSize = fontRenderingEmSize;

            GlyphIndices = glyphIndices;

            GlyphAdvances = glyphAdvances;

            GlyphOffsets = glyphOffsets;

            Characters = characters;

            GlyphClusters = glyphClusters;

            BidiLevel = bidiLevel;

            Initialize(bounds);
        }

        /// <summary>
        ///     Gets or sets the <see cref="Media.GlyphTypeface"/> for the <see cref="GlyphRun"/>.
        /// </summary>
        public GlyphTypeface GlyphTypeface
        {
            get => _glyphTypeface;
            set => Set(ref _glyphTypeface, value);
        }

        /// <summary>
        ///     Gets or sets the em size used for rendering the <see cref="GlyphRun"/>.
        /// </summary>
        public double FontRenderingEmSize
        {
            get => _fontRenderingEmSize;
            set => Set(ref _fontRenderingEmSize, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="ushort"/> values that represent the glyph indices in the rendering physical font.
        /// </summary>
        public ReadOnlySlice<ushort> GlyphIndices
        {
            get => _glyphIndices;
            set => Set(ref _glyphIndices, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="double"/> values that represent the advances corresponding to the glyph indices.
        /// </summary>
        public ReadOnlySlice<double> GlyphAdvances
        {
            get => _glyphAdvances;
            set => Set(ref _glyphAdvances, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="Vector"/> values representing the offsets of the glyphs in the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlySlice<Vector> GlyphOffsets
        {
            get => _glyphOffsets;
            set => Set(ref _glyphOffsets, value);
        }

        /// <summary>
        ///     Gets or sets the list of UTF16 code points that represent the Unicode content of the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlySlice<char> Characters
        {
            get => _characters;
            set => Set(ref _characters, value);
        }

        /// <summary>
        ///     Gets or sets a list of <see cref="int"/> values representing a mapping from character index to glyph index.
        /// </summary>
        public ReadOnlySlice<ushort> GlyphClusters
        {
            get => _glyphClusters;
            set => Set(ref _glyphClusters, value);
        }

        /// <summary>
        ///     Gets or sets the bidirectional nesting level of the <see cref="GlyphRun"/>.
        /// </summary>
        public int BidiLevel
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        internal double Scale => FontRenderingEmSize / GlyphTypeface.DesignEmHeight;

        /// <summary>
        ///     
        /// </summary>
        internal bool IsLeftToRight => ((BidiLevel & 1) == 0);

        /// <summary>
        ///     Gets or sets the conservative bounding box of the <see cref="GlyphRun"/>.
        /// </summary>
        public Rect Bounds
        {
            get
            {
                if (_bounds == null)
                {
                    _bounds = CalculateBounds();
                }

                return _bounds.Value;
            }
            set => _bounds = value;
        }

        public IGlyphRunImpl GlyphRunImpl
        {
            get
            {
                if (_glyphRunImpl == null)
                {
                    Initialize(null);
                }

                return _glyphRunImpl;
            }
        }

        public double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var distance = 0.0;

            var end = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            for (var i = 0; i < _glyphClusters.Length; i++)
            {
                if (_glyphClusters[i] >= end)
                {
                    break;
                }

                if (GlyphAdvances.IsEmpty)
                {
                    var glyph = GlyphIndices[i];

                    distance += GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
                else
                {
                    distance += GlyphAdvances[i];
                }
            }

            return distance;
        }

        public CharacterHit GetCharacterHitFromDistance(double distance, out bool isInside)
        {
            // Before
            if (distance < 0)
            {
                isInside = false;

                var firstCharacterHit = FindNearestCharacterHit(_glyphClusters[0], out _);

                return IsLeftToRight ? new CharacterHit(firstCharacterHit.FirstCharacterIndex) : firstCharacterHit;
            }

            //After
            if (distance > Bounds.Size.Width)
            {
                isInside = false;

                var lastCharacterHit = FindNearestCharacterHit(_glyphClusters[_glyphClusters.Length - 1], out _);

                return IsLeftToRight ? lastCharacterHit : new CharacterHit(lastCharacterHit.FirstCharacterIndex);
            }

            //Within
            var currentX = 0.0;
            var index = 0;

            for (; index < GlyphIndices.Length; index++)
            {
                if (GlyphAdvances.IsEmpty)
                {
                    var glyph = GlyphIndices[index];

                    currentX += GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
                else
                {
                    currentX += GlyphAdvances[index];
                }

                if (currentX > distance)
                {
                    break;
                }
            }

            if (index == GlyphIndices.Length)
            {
                index--;
            }

            var characterHit = FindNearestCharacterHit(GlyphClusters[index], out var width);

            isInside = distance < currentX && width > 0;

            var isTrailing = distance > currentX - width / 2;

            return isTrailing ? characterHit : new CharacterHit(characterHit.FirstCharacterIndex);
        }

        public CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (characterHit.TrailingLength == 0)
            {
                return FindNearestCharacterHit(characterHit.FirstCharacterIndex, out _);
            }

            var nextCharacterHit = FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

            return new CharacterHit(nextCharacterHit.FirstCharacterIndex);
        }

        public CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            return characterHit.TrailingLength == 0 ?
                FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _) :
                new CharacterHit(characterHit.FirstCharacterIndex);
        }

        private class ReverseComparer<T> : IComparer<T>
        {
            public int Compare(T x, T y)
            {
                return Comparer<T>.Default.Compare(y, x);
            }
        }

        private static readonly IComparer<ushort> s_ascendingComparer = Comparer<ushort>.Default;
        private static readonly IComparer<ushort> s_descendingComparer = new ReverseComparer<ushort>();

        internal CharacterHit FindNearestCharacterHit(int index, out double width)
        {
            width = 0.0;

            if (index < 0)
            {
                return default;
            }

            var comparer = IsLeftToRight ? s_ascendingComparer : s_descendingComparer;

            var clusters = _glyphClusters.AsSpan();

            int start;

            if (index == 0 && clusters[0] == 0)
            {
                start = 0;
            }
            else
            {
                // Find the start of the cluster at the character index.
                start = clusters.BinarySearch((ushort)index, comparer);
            }

            // No cluster found.
            if (start < 0)
            {
                while (index > 0 && start < 0)
                {
                    index--;

                    start = clusters.BinarySearch((ushort)index, comparer);
                }

                if (start < 0)
                {
                    return default;
                }
            }

            var trailingLength = 0;

            var currentCluster = clusters[start];

            while (start > 0 && clusters[start - 1] == currentCluster)
            {
                start--;
            }

            for (var lastIndex = start; lastIndex < _glyphClusters.Length; ++lastIndex)
            {
                if (_glyphClusters[lastIndex] != currentCluster)
                {
                    break;
                }

                if (GlyphAdvances.IsEmpty)
                {
                    var glyph = GlyphIndices[lastIndex];

                    width += GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
                else
                {
                    width += GlyphAdvances[lastIndex];
                }

                trailingLength++;
            }

            return new CharacterHit(currentCluster, trailingLength);
        }

        private Rect CalculateBounds()
        {
            var scale = FontRenderingEmSize / GlyphTypeface.DesignEmHeight;

            var height = (GlyphTypeface.Descent - GlyphTypeface.Ascent + GlyphTypeface.LineGap) * scale;

            var width = 0.0;

            if (GlyphAdvances.IsEmpty)
            {
                foreach (var glyph in GlyphIndices)
                {
                    width += GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
            }
            else
            {
                foreach (var advance in GlyphAdvances)
                {
                    width += advance;
                }
            }

            return new Rect(0, 0, width, height);
        }

        private void Set<T>(ref T field, T value)
        {
            if (_glyphRunImpl != null)
            {
                throw new InvalidOperationException("GlyphRun can't be changed after it has been initialized.'");
            }

            field = value;
        }

        private void Initialize(Rect? bounds)
        {
            if (GlyphIndices.Length == 0)
            {
                throw new InvalidOperationException();
            }

            var glyphCount = GlyphIndices.Length;

            if (GlyphAdvances.Length > 0 && GlyphAdvances.Length != glyphCount)
            {
                throw new InvalidOperationException();
            }

            if (GlyphOffsets.Length > 0 && GlyphOffsets.Length != glyphCount)
            {
                throw new InvalidOperationException();
            }

            _glyphRunImpl = s_platformRenderInterface.CreateGlyphRun(this, out var width);

            if (bounds.HasValue)
            {
                _bounds = bounds;
            }
            else
            {
                var height = (GlyphTypeface.Descent - GlyphTypeface.Ascent + GlyphTypeface.LineGap) * Scale;

                _bounds = new Rect(0, 0, width, height);
            }
        }

        void IDisposable.Dispose()
        {
            _glyphRunImpl?.Dispose();
        }
    }
}
